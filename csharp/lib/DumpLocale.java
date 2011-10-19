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
import java.util.Locale;
import java.util.HashMap;
/*
 * This class dumps relevant information from the java.util.Locale metadata into
 * a C# format in LocaleData.cs. Use it like:
 * $ cd csharp\lib
 * $ javac DumpLocale.java && java DumpLocale > ..\PhoneNumbers\LocaleData.cs
 */


class DumpLocale {
  private static final char SINGLE_QUOTE = 39;
  private static final char[] hexChar = {'0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f'};

  /* Print a Unicode name suitably escaped */
  private static void printName(String name) {
    System.out.print("\"");
    // Need to escape unicode data
    for (int ii=0; ii<name.length(); ii++) {
      char c = name.charAt(ii);
      if ((c >= 32) && (c < 127)) {
        if (c == SINGLE_QUOTE) {
          System.out.print("\\\"");
        } else {
          System.out.print(c);
        }
      } else {
        // non-ASCII
        System.out.print("\\u");
        System.out.print(hexChar[(c >> 12) & 0xF]);
        System.out.print(hexChar[(c >> 8) & 0xF]);
        System.out.print(hexChar[(c >> 4) & 0xF]);
        System.out.print(hexChar[c & 0xF]);
      }
    }
    System.out.print("\"");
  }

  private static void printProperty(String propName) {
    String propVal = System.getProperty(propName, null);
    if (propVal != null) {
      System.out.println("// " + propName + "=" + propVal);
    }
  }

  private static void printProlog() {
    System.out.println("// Locale information.");
    System.out.println("// Holds a map from ISO 3166-1 country code (e.g. GB) to a dict.");
    System.out.println("// Each dict maps from an ISO 639-1 language code (e.g. ja) to the country's name in that language.");
    System.out.println("//");
    System.out.println("// Generated from java.util.Locale, generation info:");
    printProperty("java.version");
    printProperty("java.vendor");
    printProperty("os.name");
    printProperty("os.arch");
    printProperty("os.version");
    System.out.println("//");
    System.out.println("// Auto-generated file, do not edit by hand.");
    System.out.println("//");
	System.out.println("using System;");
	System.out.println("using System.Collections.Generic;");
	System.out.println("//");
	System.out.println("namespace PhoneNumbers");
	System.out.println("{");
	System.out.println("  public class LocaleData");
	System.out.println("  {");
  }

  public static void main(String[] args) {
    printProlog();
    System.out.println("    public static readonly Dictionary<String, Dictionary<String, String>> Data = new Dictionary<String, Dictionary<String, String>>");
	System.out.println("    {");
    String[] all_countries = Locale.getISOCountries();
    String[] all_langs = Locale.getISOLanguages();
    // Name => first language code that maps to that name
    HashMap<String, String> name_to_lang = new HashMap<String, String>();
    for (String country: all_countries) {
      System.out.println("      {\""+country+"\", new Dictionary<String, String>");
	  System.out.println("      {");
      Locale country_locale = new Locale("", country);
      for (String lang: all_langs) {
        Locale lang_locale = new Locale(lang);
        String country_in_lang = country_locale.getDisplayCountry(lang_locale);
        if ((country_in_lang != null) && (country_in_lang.length() != 0)) {
          String previous_lang = name_to_lang.get(country_in_lang);
          if (previous_lang != null) {
            // Already seen this name before.  Print the name as "*<otherlang>"
            // on the assumption that this will save a lot of space (about 30%)
            System.out.println("        {\""+lang+"\", \"*" + previous_lang + "\"},");
          } else {
            // First time we've seen this name
            name_to_lang.put(country_in_lang, lang);
            System.out.print("        {\""+lang+"\", ");
            printName(country_in_lang);
            System.out.println("},");
          }
        }
      }
      System.out.println("      }},");
    }
	System.out.println("    };");
	System.out.println("  }");
    System.out.println("}");
  }
}