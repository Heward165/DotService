# Security policy

Please report suspected vulnerabilities privately through GitHub's security advisory interface. Do not open a public issue
for credential exposure, privilege escalation, unsafe service-path construction, DLL search-order problems, or arbitrary
command execution.

The first preview accepts only built-in, virtual, and group-managed service accounts that do not require a password.
DotService never accepts service passwords on its command line or in `dotservice.json`.

Only run installation and lifecycle commands from an elevated terminal when you understand the manifest being applied.

