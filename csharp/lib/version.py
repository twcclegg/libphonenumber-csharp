# Update assembly version number based on Java pom.xml file
import os, sys, re, subprocess

def haschanges(rootdir):
    p = subprocess.Popen(['hg', '-R', rootdir, 'id', '-n'],
                         shell=True,
                         stdout=subprocess.PIPE)
    stdout = p.communicate()[0]
    return stdout.strip().endswith('+')

def hgcat(rootdir, path, rev):
    path = os.path.join(rootdir, path)
    p = subprocess.Popen(['hg', 'cat', '--rev', rev, path],
                         shell=True,
                         stdout=subprocess.PIPE)
    stdout = p.communicate()[0]
    if p.returncode:
        raise Exception('hg cat failed')
    return stdout

def readjavaver(rootdir, rev):
    # Extract from pom file
    data = hgcat(rootdir, 'java/pom.xml', rev)
    m = re.search(r'<version>(\d+.\d+)(-SNAPSHOT)?</version>', data)
    if not m:
        raise Exception('cannot extract version number from pom file')
    snapshot = bool(m.group(2))
    return tuple(int(p) for p in m.group(1).split('.'))[:2], snapshot

def getjavaver(rootdir):
    rev = '.'
    while True:
        ver, snapshot = readjavaver(rootdir, rev)
        if not snapshot:
            return ver
        rev = 'p1(%s)' % rev

asmfile = 'csharp/PhoneNumbers/Properties/AssemblyInfo.cs'

def getcsharpprevbuild(rootdir):
    stdout = hgcat(rootdir, asmfile, '.')
    m = re.search(r'AssemblyVersion\("(\d+\.\d+\.\d+\.\d+)', stdout)
    if not m:
        raise Exception('cannot extract version from AssemblyInfo.cs')
    build = int(m.group(1).split('.')[3])
    return build

def updatecsharpver(rootdir, ver):
    asmpath = os.path.join(rootdir, asmfile)
    data = file(asmpath, 'rb').read()
    ver = (ver + (0, 0, 0, 0))[:4]
    ver = '.'.join(str(p) for p in ver)
    rewritten, n = re.subn(r'((?:AssemblyVersion|AssemblyFileVersion)\(")([^"]+)',
                           r'\g<1>' + ver, data)
    if not n:
        raise Exception('cannot extract version from AssemblyInfo.cs')
    if rewritten != data:
        print 'Updating to', ver
        file(asmpath, 'wb').write(rewritten)

if __name__ == '__main__':
    rootdir = os.path.join(os.path.dirname(__file__), '../..')
    if haschanges(rootdir):
        # Version number is only updates when merging and building
        # locally. Do not update it when building against a clean
        # existing revision.
        ver = getjavaver(rootdir)
        build = getcsharpprevbuild(rootdir)
        ver = ver[:2] + (0, build + 1)
        updatecsharpver(rootdir, ver)
    
    
