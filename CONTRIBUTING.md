# Contributing to libphonenumber-csharp

Thank you for considering contributing to libphonenumber-csharp! You can contribute to libphonenumber-csharp with issues and PRs.

Simply filing issues for problems you encounter or contributing any implementation of an issue is greatly appreciated.

### Suggested Workflow

We use and recommend the following workflow:

1. Create an issue for your work.
    - You can skip this step for trivial changes.
    - Reuse an existing issue on the topic, if there is one.
    - For trivial changes, feel free to start work without an agreement from the maintainers.
    - For slightly larger changes, you can reach out to the maintainers to discuss the change.
    - Clearly state that you are going to take on implementing it if you are planning to, you can request that the issue be assigned to you.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. In your fork, create a branch off of main (`git checkout -b mybranch`).
    - Name the branch so that it clearly communicates your intentions, such as issue-123 or githubhandle-issue.
4. Make and commit your changes to your branch.
5. Add new tests corresponding to your change, if applicable.
6. Build the repository with your changes.
    - Make sure that the builds are clean.
    - Make sure that the tests are all passing, including your new tests.
7. Create a pull request (PR) against the **main** branch.
    - State in the description what issue or improvement your change is addressing.
    - Check if all the Continuous Integration checks are passing.
8. Wait for feedback or approval of your changes from the maintainers
9. Maintainers will merge once all checks are green and they are happy with the change
    - The next official build will automatically include your change.

Essentially, we are following trunk based development
