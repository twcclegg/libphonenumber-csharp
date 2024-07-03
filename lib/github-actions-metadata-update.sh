#! /bin/bash
# Exit bash script on any command that returns a non zero error code
set -e

if [ $# -ne 1 ]
then
    echo "GitHub token required"
    exit 123
fi

if [ ! command -v jq &> /dev/null ]
then
    echo "jq required"
    exit 123
fi

getLatestGitHubRelease() {
    curl "https://api.github.com/repos/$1/releases/latest" | jq -r .tag_name
}

getLatestNugetRelease() {
    curl "https://www.nuget.org/packages/$1/" | grep 'og:title' | sed "s/.*$1 \([^\"]*\).*/\1/"
}

getReleaseDelta() {
    curl https://api.github.com/repos/$1/compare/$2...$3 | jq .files[].filename
}

createRelease() {
    curl -f -H "Authorization: Bearer $GITHUB_TOKEN" -d "{\"tag_name\":\"$2\",\",name\":\"$2\"}" "https://api.github.com/repos/$1/releases"
}

GITHUB_TOKEN=$1
UPSTREAM_GITHUB_RELEASE_TAG=$(getLatestGitHubRelease google/libphonenumber)
DEPLOYED_NUGET_TAG=$(getLatestNugetRelease libphonenumber-csharp)
GITHUB_REPOSITORY_OWNER=twcclegg
GITHUB_REPOSITORY_NAME=libphonenumber-csharp
GITHUB_ACTION_WORKING_DIRECTORY=$(pwd)

echo "google/libphonenumber latest release is ${UPSTREAM_GITHUB_RELEASE_TAG}"
echo "libphonenumber-csharp latest release is ${DEPLOYED_NUGET_TAG}"

if [ "${UPSTREAM_GITHUB_RELEASE_TAG:1:1}" != "8" ]
then
    echo "major version update"
    exit 123
fi

if [ "$DEPLOYED_NUGET_TAG" = "${UPSTREAM_GITHUB_RELEASE_TAG:1}" ]
then
    echo "versions match, new release not required"
    exit 0
fi

mkdir ~/GitHub

cd ~/GitHub
git clone https://github.com/${GITHUB_REPOSITORY_OWNER}/${GITHUB_REPOSITORY_NAME}.git
cd ${GITHUB_REPOSITORY_NAME}
git checkout main

cd ~/GitHub/$GITHUB_REPOSITORY_NAME
if [ $(git branch --show-current) != "main" ]
then
    echo "must be on main branch"
    exit 123
fi

if [ -n "$(git status --porcelain)" ]
then
    echo "working directory is not clean"
    exit 123
fi

cd ~/GitHub
git clone https://github.com/google/libphonenumber.git
cd libphonenumber
git checkout master

cd ~/GitHub/libphonenumber
PREVIOUS=$(git describe --abbrev=0)

FILES=$(getReleaseDelta google/libphonenumber "v${DEPLOYED_NUGET_TAG}" $UPSTREAM_GITHUB_RELEASE_TAG)

if echo $FILES | grep '\.java'
then
   # echo "has java files, automatic update not possible"
   # exit 123
fi

if echo $FILES | grep 'proto'
then
   echo "has proto files, automatic update not possible"
   exit 123
fi

git config --global user.email '<>'
git config --global user.name 'libphonenumber-csharp-bot'

git fetch origin
git reset --hard $UPSTREAM_GITHUB_RELEASE_TAG
rm -rf ${GITHUB_ACTION_WORKING_DIRECTORY}/resources/*
cp -r resources/* ${GITHUB_ACTION_WORKING_DIRECTORY}/resources
cd ${GITHUB_ACTION_WORKING_DIRECTORY}
cd lib
javac DumpLocale.java && java DumpLocale > ../csharp/PhoneNumbers/LocaleData.cs
rm DumpLocale.class

# Ensure project builds and passes tests before committing
cd ${GITHUB_ACTION_WORKING_DIRECTORY}
(cd resources/geocoding; zip -r ../../resources/geocoding.zip *)
(cd resources/test/geocoding; zip -r ../../../resources/test/testgeocoding.zip *)
cd csharp
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal -p:TargetFrameworks=net7.0
# Cleanup test dependencies
rm -rf ${GITHUB_ACTION_WORKING_DIRECTORY}/resources/geocoding.zip
rm -rf ${GITHUB_ACTION_WORKING_DIRECTORY}/resources/test/testgeocoding.zip

cd ${GITHUB_ACTION_WORKING_DIRECTORY}
git add -A
git commit -m "feat: automatic upgrade to ${UPSTREAM_GITHUB_RELEASE_TAG}"
git push

createRelease ${GITHUB_REPOSITORY_OWNER}/${GITHUB_REPOSITORY_NAME} $UPSTREAM_GITHUB_RELEASE_TAG
