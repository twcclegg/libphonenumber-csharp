import os, re, shutil

def copygeocoding(source, prefix, dest):
    entries = []
    for root, dirs, files in os.walk(source):
        for f in files:
            if not re.search(r'^\d+\.txt$', f):
                continue
            country = f[:-4]
            s = os.path.join(root, f)
            lang = os.path.split(root)[-1]
            fn = '%s%s_%s' % (prefix, country, lang)
            t = os.path.join(dest, fn)
            opts = ''
            if os.path.exists(t):
                print t
                print s
                datasrc = file(s, 'rb').read()
                datadst = file(t, 'rb').read()
                if datasrc == datadst:
                    continue
                opts = ' --force '
            print 'hg cp %s %s %s' % (opts, s, t)

if __name__ == '__main__':
    rootpath = os.path.join(os.path.dirname(__file__), '../../')
    dest = os.path.join(rootpath, 'csharp/PhoneNumbers/res')
    if not os.path.exists(dest):
        os.makedirs(dest)
    sources = [
        (os.path.join(rootpath, 'resources/geocoding'), ''),
        (os.path.join(rootpath, 'resources/test/geocoding'), 'test_'),
        ]
    for source, prefix in sources:
        copygeocoding(source, prefix, dest)
