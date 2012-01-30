import os, subprocess, shutil

class Ui:
    def __init__(self):
        rootdir = os.path.join(os.path.dirname(__file__), '../..')
        rootdir = os.path.abspath(rootdir)
        self.rootdir = rootdir

    def nuget(self, args):
        nuget = os.path.join(ui.rootdir, 'csharp/lib/nuget.exe')
        r = subprocess.call([nuget] + args)
        return r

def haschanges(ui):
    p = subprocess.Popen(['hg', '-R', ui.rootdir, 'id', '-n'],
                         shell=True,
                         stdout=subprocess.PIPE)
    stdout = p.communicate()[0]
    return stdout.strip().endswith('+')

def purge(ui):
    subprocess.check_call(['hg', '-R', ui.rootdir, 'purge', '--all',
                           '-X', 'csharp/PhoneNumbers.Test'],
                          shell=True)

def build(ui):
    csproj = os.path.join(ui.rootdir, 'csharp/PhoneNumbers/PhoneNumbers.csproj')
    outdir = os.path.join(ui.rootdir, 'csharp/packages')
    if os.path.exists(outdir):
        shutil.rmtree(outdir)
    os.makedirs(outdir)
    r = ui.nuget(['pack', csproj,
                  '-Properties', 'Configuration=Release',
                  '-OutputDirectory', outdir,
                  '-verbose',
                  '-symbols',
                  '-build'])
    if r:
        raise Exception('nuget pack failed')

def push(ui):
    outdir = os.path.join(ui.rootdir, 'csharp/packages')
    nupkg = [f for f in sorted(os.listdir(outdir)) if f.endswith('.nupkg')][0]
    nupkg = os.path.join(outdir, nupkg)
    ui.nuget(['push', nupkg])

if __name__ == '__main__':
    ui = Ui()
    if haschanges(ui):
        raise Exception('abort: local changes found')
    purge(ui)
    build(ui)
    push(ui)
