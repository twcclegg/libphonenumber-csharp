#! /bin/bash

if [ $# -ne 1 ]
then
    echo "GitHub token required"
    exit
fi

if [ ! command -v jq &> /dev/null ]
then
    echo "jq required"
    exit
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

getAppVeyorStatus() {
    curl -s https://ci.appveyor.com/api/projects/$1 | jq -r .build.status
}

createRelease() {
    curl -f -H "Authorization: Bearer $GITHUB_TOKEN" -d "{\"tag_name\":\"$2\",\",name\":\"$2\"}" "https://api.github.com/repos/$1/releases"
}

GITHUB_TOKEN=$1
UPSTREAM=$(getLatestGitHubRelease google/libphonenumber)
DEPLOYED=$(getLatestNugetRelease libphonenumber-csharp)

if [ "$DEPLOYED" = "${UPSTREAM:1}" ]
then
    echo "versions match"
    exit
fi

cd ~/GitHub/libphonenumber-csharp/
if [ $(git branch --show-current) != "main" ]
then
    echo "must be on main branch"
    exit
fi

if [ -n "$(git status --porcelain)" ]
then
    echo "working directory is not clean"
    #exit
fi

cd ~/GitHub/libphonenumber/
PREVIOUS=$(git describe --abbrev=0)

FILES=$(getReleaseDelta google/libphonenumber $PREVIOUS $UPSTREAM)

if echo $FILES | grep '\.java'
then
   echo "has java"
   #exit
fi

if echo $FILES | grep 'proto'
then
   echo "has proto"
   exit
fi

git fetch origin
git reset --hard $UPSTREAM
rm -rf ../libphonenumber-csharp/resources/*
cp -r resources/* ../libphonenumber-csharp/resources
cd ../libphonenumber-csharp
cd lib
javac DumpLocale.java && java DumpLocale > ../csharp/PhoneNumbers/LocaleData.cs
rm DumpLocale.class
git add -A
git commit -m "$UPSTREAM"
git push
sleep 15
echo -n "build pending"
sleep 60

while
    sleep 15
    echo -n "."
    RESULT=$(getAppVeyorStatus twcclegg/libphonenumber-csharp)
    [ $RESULT = "running" ] || [ $RESULT = "starting" ]
do true
done
echo

if [ $RESULT != "success" ]
then
    echo "build failed: $RESULT"
    exit
fi

createRelease twcclegg/libphonenumber-csharp $UPSTREAM
