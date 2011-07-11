import re, sys

replacements = [
    ('.length()', '.Length'),
    ('.containsKey(', '.ContainsKey('),
    ('.substring(', '.Substring('),
    ('.setCountryCode(', '.SetCountryCode('),
    ('.setNationalNumber(', '.SetNationalNumber('),
    ('assertEquals(', 'Assert.AreEqual('),
    ('assertFalse(', 'Assert.False('),
    ('assertTrue(', 'Assert.True('),
    ('.getDescriptionForNumber(', '.GetDescriptionForNumber('),
    ('.hasAttribute(', '.HasAttribute('),
    ('.getAttribute(', '.GetAttribute('),
    ('.equals(', '.Equals('),
    ('.getNationalPrefixForParsing()', '.NationalPrefixForParsing'),
    ('.getFormat()', '.Format'),
]

regexps = [
    (r'public\s+void\s+test(\S+)\(\)(?:\s+throws\s+\S+)?\s*{',
     r'[Test]\npublic void Test\1()\n{',
     re.S),
]

def fixcsharp(path):
    data = file(path, 'rb').read()
    for match, sub in replacements:
        data = data.replace(match, sub)
    for match, sub, opts in regexps:
        regex = re.compile(match, opts)
        data = regex.sub(sub, data)
    data = data.replace('\r\n', '\n')
    file(path, 'wb').write(data)
    
if __name__ == '__main__':
    for path in sys.argv[1:]:
        fixcsharp(path)
