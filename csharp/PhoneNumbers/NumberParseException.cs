/*
 * Copyright (C) 2009 Google Inc.
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

using System;

namespace PhoneNumbers
{
    /// The reason that a string could not be interpreted as a phone number.
    public enum ErrorType
    {
        /// The country code supplied did not belong to a supported country or non-geographical entity.
        INVALID_COUNTRY_CODE,
        /// This indicates the string passed is not a valid number. Either the string had less than 3
        /// digits in it or had an invalid phone-context parameter. More specifically, the number failed
        /// to match the regular expression VALID_PHONE_NUMBER, RFC3966_GLOBAL_NUMBER_DIGITS, or
        /// RFC3966_DOMAINNAME in PhoneNumberUtil.cs.
        NOT_A_NUMBER,
        // This indicates the string started with an international dialing prefix, but after this was
        // stripped from the number, had less digits than any valid phone number (including country
        // code) could have.
        TOO_SHORT_AFTER_IDD,
        // This indicates the string, after any country code has been stripped, had less digits than any
        // valid phone number could have.
        TOO_SHORT_NSN,
        // This indicates the string had more digits than any valid phone number could have.
        TOO_LONG
    }

    public class NumberParseException : Exception
    {
        public readonly ErrorType ErrorType;

        public NumberParseException(ErrorType errorType, string message) :
            base(message)
        {
            ErrorType = errorType;
        }
    }
}
