import { spawnSync } from 'node:child_process'
import { mkdirSync, readFileSync } from 'node:fs'
import path from 'node:path'

function runDevCertificateCommand(args: string[], failureMessage: string) {
  const result = spawnSync('dotnet', ['dev-certs', 'https', ...args], {
    encoding: 'utf8',
    windowsHide: true,
  })

  if (result.status === 0) {
    return
  }

  const details = [
    result.error?.message,
    result.stdout,
    result.stderr,
  ].filter(Boolean).join('\n').trim()

  throw new Error(details ? `${failureMessage}\n${details}` : failureMessage)
}

export function aspNetDevelopmentHttps(cacheDirectory: string) {
  runDevCertificateCommand(
    ['--check', '--trust'],
    'Concertable frontend HTTPS requires a trusted ASP.NET development certificate. Run: dotnet dev-certs https --trust',
  )

  mkdirSync(cacheDirectory, { recursive: true })

  const certificatePath = path.join(cacheDirectory, 'localhost.pem')
  const keyPath = path.join(cacheDirectory, 'localhost.key')

  runDevCertificateCommand(
    ['--export-path', certificatePath, '--format', 'Pem', '--no-password'],
    'Could not export the ASP.NET development certificate for the Vite dev server.',
  )

  return {
    cert: readFileSync(certificatePath),
    key: readFileSync(keyPath),
  }
}
