const fs = require('node:fs')
const path = require('node:path')
const root = path.join(__dirname, '..', 'desktop')
const names = ['react', 'react-dom', 'radix-ui', 'shadcn', 'lucide-react', '@fontsource-variable/inter', '@fontsource-variable/lora', 'class-variance-authority', 'clsx', 'tailwind-merge']
let content = '# Third-party notices\n\nKnowYourDevice bundles the following open-source UI libraries and fonts. Electron and Chromium license notices are also included in the application distribution by electron-builder.\n\n'
for (const name of names) {
  const directory = path.join(root, 'node_modules', name)
  const metadata = JSON.parse(fs.readFileSync(path.join(directory, 'package.json'), 'utf8'))
  const license = fs.readdirSync(directory).find(file => /^licen[sc]e(\.md|\.txt)?$/i.test(file))
  if (!license) throw new Error(`License file missing: ${name}`)
  content += `## ${name} ${metadata.version}\n\n\`\`\`text\n${fs.readFileSync(path.join(directory, license), 'utf8').trim()}\n\`\`\`\n\n`
}
fs.writeFileSync(path.join(root, 'THIRD_PARTY_NOTICES.md'), content.trimEnd() + '\n')
