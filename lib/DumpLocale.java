/*
 * Copyright (C) 2010-2011 David Drysdale <dmd@lurklurk.org>
 * Copyright (C) 2011 Patrick Mezard <pmezard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
import java.io.PrintStream;
import java.io.UnsupportedEncodingException;
import java.util.Locale;
import java.util.HashMap;
/*
 * Dumps country names per language from java.util.Locale as the pipe-delimited text that
 * PhoneNumbers.MetadataBuilder turns into per-country binary resources. Use it like:
 * $ javac DumpLocale.java && java DumpLocale > ../resources/locale/country_names.txt
 */


class DumpLocale {
  private static PrintStream out;

  /*
   * Names are written as-is. The delimiter and newline are the only characters that would break
   * the format, and no java.util.Locale country name contains either; bail loudly rather than
   * emit a file the builder would silently misparse.
   */
  private static void printName(String name) {
    if (name.indexOf('|') >= 0 || name.indexOf('\n') >= 0 || name.indexOf('\r') >= 0) {
      throw new IllegalStateException("country name is not representable in this format: " + name);
    }
    out.print(name);
  }

  private static void printProperty(String propName) {
    String propVal = System.getProperty(propName, null);
    if (propVal != null) {
      out.println("# " + propName + "=" + propVal);
    }
  }

  private static void printProlog() {
    out.println("# Country names by language, generated from java.util.Locale by lib/DumpLocale.java.");
    out.println("# Format: <ISO 3166-1 country>|<ISO 639-1 language>|<name>");
    out.println("# A name of *xx means 'the name for language xx in this country', stored once.");
    out.println("#");
    out.println("# Auto-generated file, do not edit by hand. Generation info:");
    printProperty("java.version");
    printProperty("java.vendor");
  }

  public static void main(String[] args) throws UnsupportedEncodingException {
    // Names are non-ASCII for most languages, and the file is read back as UTF-8.
    out = new PrintStream(System.out, true, "UTF-8");
    printProlog();
    String[] all_countries = Locale.getISOCountries();
    String[] all_langs = Locale.getISOLanguages();
    for (String country: all_countries) {
      // Name => first language code that maps to that name, for this country only. Sharing one
      // map across countries lets a name claimed by an earlier country alias a language the
      // current one has no entry for, which resolves to nothing at lookup time.
      HashMap<String, String> name_to_lang = new HashMap<String, String>();
      Locale country_locale = new Locale("", country);
      for (String lang: all_langs) {
        Locale lang_locale = new Locale(lang);
        String country_in_lang = country_locale.getDisplayCountry(lang_locale);
        if ((country_in_lang != null) && (country_in_lang.length() != 0)) {
          String previous_lang = name_to_lang.get(country_in_lang);
          out.print(country);
          out.print("|");
          out.print(lang);
          out.print("|");
          if (previous_lang != null) {
            // Already seen this name before. Store it as "*<otherlang>"; this saves about 30%.
            out.print("*");
            out.println(previous_lang);
          } else {
            // First time we've seen this name
            name_to_lang.put(country_in_lang, lang);
            printName(country_in_lang);
            out.println();
          }
        }
      }
    }
  }
}
