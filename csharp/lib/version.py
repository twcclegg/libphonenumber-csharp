# Update assembly version number based on Java pom.xml file
import os, re, subprocess

def getjavaver(rootdir):
    # Extract from pom file
    pompath = os.path.join(rootdir, 'java', 'pom.xml')
    data = file(pompath, 'rb').read()
    m = re.search(r'<version>(\d+.\d+)(?:-SNAPSHOT)?</version>', data)
    if not m:
        raise Exception('cannot extract version number from pom file')
    return tuple(int(p) for p in m.group(1).split('.'))[:2]

asmfile = 'csharp/PhoneNumbers/Properties/AssemblyInfo.cs'

def getcsharpprevbuild(rootdir):
    asmpath = os.path.join(rootdir, asmfile)
    p = subprocess.Popen(['hg', 'cat', '--rev', '.', asmpath],
                         shell=True,
                         stdout=subprocess.PIPE)
    stdout = p.communicate()[0]
    if p.returncode:
        raise Exception('hg cat failed')
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
    ver = getjavaver(rootdir)
    build = getcsharpprevbuild(rootdir)
    ver = ver[:2] + (0, build + 1)
    updatecsharpver(rootdir, ver)
    
    
