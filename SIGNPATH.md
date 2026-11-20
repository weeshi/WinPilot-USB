# SignPath Foundation

This repository is prepared for the SignPath Foundation free code-signing program.
The repository must remain public and releases must be built by GitHub Actions.
Commercial/offline installers are intentionally excluded from Git because their
redistribution rights and size vary; place them in `Apps` only for a local USB bundle.

After acceptance, configure the repository secrets and variables documented in
`.github/workflows/release.yml`, then create a version tag such as `v1.0.1`.
SignPath signs only the build artifact produced by the trusted workflow.
