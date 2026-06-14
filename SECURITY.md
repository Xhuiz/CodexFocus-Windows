# Security Policy

Codex Focus Windows reads local Codex Desktop session transcript files to infer task state and uses Windows window activation to switch between Codex Desktop and the Douyin Windows client. It should not upload user data or operate Codex prompt input.

## Supported Versions

| Version | Supported |
| --- | --- |
| 0.1.x | Yes |

## Reporting A Vulnerability

If you find a security issue, please avoid posting private prompts, transcript contents, credentials, tokens, or personal data in public.

Open a GitHub issue with minimal reproduction details, or contact the maintainer through the GitHub profile if the report needs sensitive context:

- Issues: https://github.com/Xhuiz/CodexFocus-Windows/issues
- Maintainer: https://github.com/Xhuiz

## Relevant Security Boundaries

- The app should only read local Codex transcript files required to infer task state.
- The app should not upload Codex transcripts.
- The app should not send prompts to Codex or modify Codex input.
- Window switching should stay local to the Windows desktop session.