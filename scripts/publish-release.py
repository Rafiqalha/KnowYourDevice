"""Publish reviewed local artifacts using existing Git credentials; never print credentials."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import urllib.error
import urllib.parse
import urllib.request

REPOSITORY = 'Rafiqalha/KnowYourDevice'
API = f'https://api.github.com/repos/{REPOSITORY}'


def credentials():
    token = os.environ.get('GH_TOKEN') or os.environ.get('GITHUB_TOKEN')
    if token:
        return token
    result = subprocess.run(['git', 'credential', 'fill'], input='protocol=https\nhost=github.com\n\n', text=True, capture_output=True, check=True)
    fields = dict(line.split('=', 1) for line in result.stdout.splitlines() if '=' in line)
    if not fields.get('password'):
        raise RuntimeError('Autentikasi GitHub tidak tersedia dari Git Credential Manager.')
    return fields['password']


def request(url, token, method='GET', payload=None, binary=None):
    headers = {'Authorization': f'Bearer {token}', 'Accept': 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28', 'User-Agent': 'KnowYourDevice-release'}
    data = None
    if payload is not None:
        data = json.dumps(payload).encode()
        headers['Content-Type'] = 'application/json'
    if binary is not None:
        data = binary.read_bytes()
        headers['Content-Type'] = 'application/octet-stream'
    try:
        with urllib.request.urlopen(urllib.request.Request(url, data=data, headers=headers, method=method), timeout=300) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        if error.code == 404 and method == 'GET':
            return None
        raise RuntimeError(f'GitHub API HTTP {error.code}: {error.reason}') from None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--commit', required=True)
    parser.add_argument('--inspect-only', action='store_true')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    version = json.loads((root / 'desktop/package.json').read_text())['version']
    token = credentials()
    repo = request(API, token)
    if not repo:
        raise RuntimeError('Repositori tujuan tidak ditemukan atau tidak dapat diakses.')
    tag = f'v{version}'
    print(json.dumps({'repository': REPOSITORY, 'private': repo['private'], 'version': version}))
    if args.inspect_only:
        return
    # Publishing a private repository does not change its visibility.
    commit = request(f'{API}/git/commits/{args.commit}', token)
    if not commit or commit['sha'] != args.commit:
        raise RuntimeError('Commit sumber belum tersedia di GitHub.')
    files = [root / 'dist' / f'KnowYourDevice-{version}-Windows-x64.exe', root / 'dist/SHA256SUMS.txt']
    for file in files:
        if not file.is_file() or file.stat().st_size == 0:
            raise RuntimeError(f'Artifact tidak tersedia: {file.name}')
    expected_checksum = hashlib.sha256(files[0].read_bytes()).hexdigest()
    if expected_checksum not in files[1].read_text(encoding='utf-8-sig'):
        raise RuntimeError('Checksum artifact tidak cocok.')
    release = request(f'{API}/releases/tags/{tag}', token)
    if not release:
        release = request(f'{API}/releases', token, 'POST', {'tag_name': tag, 'target_commitish': args.commit, 'name': f'KnowYourDevice {version}', 'body': (root / f'docs/releases/{tag}.md').read_text(encoding='utf-8'), 'draft': True, 'prerelease': False})
    ref = request(f'{API}/git/ref/tags/{tag}', token)
    if ref and ref['object']['sha'] != args.commit:
        raise RuntimeError('Tag rilis yang ada menunjuk commit lain; tidak akan ditimpa.')
    upload = release['upload_url'].split('{')[0]
    for file in files:
        existing = next((asset for asset in release['assets'] if asset['name'] == file.name), None)
        if existing:
            digest = 'sha256:' + hashlib.sha256(file.read_bytes()).hexdigest()
            if existing.get('digest') != digest:
                raise RuntimeError(f'Artifact {file.name} sudah ada dengan digest berbeda atau belum dapat diverifikasi.')
            continue
        asset = request(upload + '?name=' + urllib.parse.quote(file.name), token, 'POST', binary=file)
        expected_digest = 'sha256:' + hashlib.sha256(file.read_bytes()).hexdigest()
        if asset['size'] != file.stat().st_size or (asset.get('digest') and asset['digest'] != expected_digest):
            raise RuntimeError(f'Verifikasi unggahan gagal: {file.name}')
        print(json.dumps({'uploaded': asset['name'], 'bytes': asset['size'], 'digest': asset.get('digest')}))
    release = request(f"{API}/releases/{release['id']}", token, 'PATCH', {'draft': False, 'make_latest': 'true'})
    verified = request(f'{API}/releases/tags/{tag}', token)
    if verified['draft'] or len([asset for asset in verified['assets'] if asset['name'] in [file.name for file in files]]) != len(files):
        raise RuntimeError('Rilis publik belum lengkap.')
    print(json.dumps({'release': verified['html_url'], 'assets': [{'name': asset['name'], 'url': asset['browser_download_url'], 'size': asset['size'], 'digest': asset.get('digest')} for asset in verified['assets']]}))


if __name__ == '__main__':
    main()
