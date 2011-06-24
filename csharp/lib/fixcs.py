import re, sys

replacements = [
    ('.length()', '.Length'),
    ('.containsKey(', '.ContainsKey('),
    ('.substring(', '.Substring('),
    ('.setCountryCode(', '.SetCountryCode('),
    ('.setNationalNumber(', '.SetNationalNumber('),
    ('assertEquals(', 'Assert.AreEqual('),
    ('.getDescriptionForNumber(', '.GetDescriptionForNumber('),
]

def fixcsharp(path):
    data = file(path, 'rb').read()
    for match, sub in replacements:
        data = data.replace(match, sub)
    file(path, 'wb').write(data)
    
if __name__ == '__main__':
    for path in sys.argv[1:]:
        fixcsharp(path)
