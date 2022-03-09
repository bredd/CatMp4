/*
---
name: CommandLineLexer.cs
description: CodeBit class for lexing (parsing) command lines.
url: https://raw.githubusercontent.com/bredd/CommandLineLexer/main/CommandLineLexer.cs
version: 1.2
keywords: CodeBit
dateModified: 2021-03-22
license: https://opensource.org/licenses/BSD-3-Clause
# Metadata in MicroYaml format. See http://filemeta.org/CodeBit.html
...
*/

/*
=== BSD 3 Clause License ===
https://opensource.org/licenses/BSD-3-Clause

Copyright 2021 Brandt Redd

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
this list of conditions and the following disclaimer in the documentation
and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors
may be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CodeBit
{
    /// <summary>
    /// Yet another command-line parsing helper class. This one
    /// uses a lexer structure and expects the application to read the
    /// command-line options in sequence. Distinction from a simple
    /// foreach loop on 'args' is that this class makes error reporting easier
    /// by tracking the context.
    /// </summary>
    class CommandLineLexer : IEnumerator<string>
    {
        string[] m_args;
        int m_currentArg;
        string m_latestOption;

        /// <summary>
        /// Initialize with the command line from the environment
        /// </summary>
        /// <param name="acceptEmpty">If true, accept an empty command line. Else throws an exception.</param>
        public CommandLineLexer(bool acceptEmpty=false)
            : this(Environment.GetCommandLineArgs(), acceptEmpty)
        {
        }

        /// <summary>
        /// Initialize with the specified arguments.
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        /// <param name="acceptEmpty">If true, accept an empty command line. Else throws an exception.</param>
        public CommandLineLexer(string[] args, bool acceptEmpty=false)
        {
            m_args = args;
            if (m_args.Length == 0 && !acceptEmpty)
            {
                throw new CommandLineException("Empty Command Line.");
            }
            Reset();
        }

        /// <summary>
        /// Reset to the beginning of the command line
        /// </summary>
        public void Reset()
        {
            m_currentArg = -1;
            m_latestOption = string.Empty;
        }

        /// <summary>
        /// The current argument as a string
        /// </summary>
        public string Current
        {
            get
            {
                if (m_currentArg < 0) throw new InvalidOperationException("CommandLineLexer: Must call MoveNext() before Current");
                if (m_currentArg >= m_args.Length) throw new InvalidOperationException("CommandLineLexer: All arguments have been read.");
                return m_args[m_currentArg];
            }
        }

        /// <summary>
        /// Indicates whether the current argument is an option.
        /// </summary>
        /// <remarks>
        /// A command-line argument starts with a dash.
        /// </remarks>
        public bool IsOption
        {
            get
            {
                return m_currentArg < m_args.Length && m_args[m_currentArg][0] == '-';
            }
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Returns the current argument as an integer.
        /// </summary>
        /// <exception cref="CommandLineException">Thrown if the value is not an integer.</exception>
        public int CurrentInt
        {
            get
            {
                int value;
                if (!int.TryParse(Current, out value))
                    throw new CommandLineException($"{OptionErrPrefix}Expected integer; found \"{Current}\".");
                return value;
            }
        }

        /// <summary>
        /// The latest option is the most recent argument with a - (dash) prefix.
        /// </summary>
        /// <remarks>
        /// This is useful for error reporting with an invalid value. It may also be cleared
        /// (by setting to null or empty string) or set to a special value.
        /// </remarks>
        public string LatestOption
        {
            get
            {
                return m_latestOption;
            }

            set
            {
                m_latestOption = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Disposal is not required - does nothing
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <summary>
        /// Moves to the next argument. Returns true if another argument is present. Otherwise, false.
        /// </summary>
        /// <returns>True if another argument was read. Else, false.</returns>
        public bool MoveNext()
        {
            if (m_currentArg >= m_args.Length) return false;
            ++m_currentArg;
            if (m_currentArg >= m_args.Length)
            {
                m_latestOption = string.Empty;
                return false;
            }
            if (IsOption)
            {
                m_latestOption = Current;
            }
            return true;
        }

        /// <summary>
        /// Moves to the next option
        /// </summary>
        /// <remarks>
        /// Throws an <see cref="CommandLineException"/> if the next argument is is NOT an option
        /// (prefixed with a hyphen). When an exception is thrown, the current value does not advance.
        /// </remarks>
        /// <returns>True if moved to an option. False if at the end of the argument set.</returns>
        /// <exception cref="CommandLineException">Thrown if the next argument is NOT an option.</exception>
        public bool MoveNextOption()
        {
            if (m_currentArg >= m_args.Length - 1)
            {
                m_currentArg = m_args.Length;
                m_latestOption = string.Empty;
                return false; 
            }
            if (m_args[m_currentArg + 1].Length <= 0 || m_args[m_currentArg + 1][0] != '-')
                ThrowValueError($"Option expected. Found \"{m_args[m_currentArg + 1]}\"");
            ++m_currentArg;
            return true;
        }

        /// <summary>
        /// Moves to the next value, presumably for the current option.
        /// </summary>
        /// <remarks>
        /// Throws an <see cref="CommandLineException"/> if there are no more arguments or if the next
        /// argument is is an option (prefixed with a hyphen). When an exception is thrown, the current
        /// value does not advance.
        /// </remarks>
        /// <exception cref="CommandLineException">Thrown if at the end of the argument string
        /// or the next argument is an option.</exception>
        public void MoveNextValue()
        {
            if (m_currentArg >= m_args.Length - 1)
                ThrowValueError("Value expected.");
            if (m_args[m_currentArg+1].Length > 0 && m_args[m_currentArg+1][0] == '-')
                ThrowValueError($"Value expected. Found \"{m_args[m_currentArg + 1]}\"");
            ++m_currentArg;
        }

        /// <summary>
        /// Moves to the next argument and returns it.
        /// </summary>
        /// <returns>The next argument or null if at the end of the argument list.</returns>
        public string ReadNextArg()
        {
            if (!MoveNext()) return null;
            return Current;
        }

        /// <summary>
        /// Moves to the next option and returns it.
        /// </summary>
        /// <returns>The next option or null if at the end of the argument list.</returns>
        /// <remarks>
        /// <para>The next argument must be an option, not a value. That is, it must be
        /// prefixed with a hyphen. If the next argument does not have a hyphen prefix
        /// then a <see cref="CommandLineException"/> is thrown. If there are no more
        /// arguments then null is returned.</para>
        /// <para>The null return behavior is in contrast with <see cref="ReadNextValue"/>
        /// which throws an exception if there are no more arguments. But it makes sense
        /// in the context in which each method is used.</para>
        /// </remarks>
        /// <returns>The next option in the argument list or null if no arguments remain.</returns>
        /// <exception cref="CommandLineException">Thrown if the next argument is not an option.</exception>
        public string ReadNextOption()
        {
            if (!MoveNextOption()) return null;
            return Current;
        }

        /// <summary>
        /// Advances to the next value and returns it.
        /// </summary>
        /// <returns>The string value of the next argument.</returns>
        /// <remarks>
        /// The next argument must be a value, not an option. That is, it must not be
        /// prefixed with a hyphen. If at the end of the argument string or the value
        /// is an option then an <see cref="CommandLineException"/> is thrown.
        /// </remarks>
        /// <exception cref="CommandLineException">Thrown if at the end of the argument string
        /// or the next argument is an option.</exception>
        public string ReadNextValue()
        {
            MoveNextValue();
            return Current;
        }

        /// <summary>
        /// Advances to the next value and returns it as an integer.
        /// </summary>
        /// <returns>The string value of the next argument.</returns>
        /// <remarks>
        /// The next argument must be a value, not an option. That is, it must not be
        /// prefixed with a hyphen. If at the end of the argument string or the value
        /// is an option then an <see cref="CommandLineException"/> is thrown.
        /// </remarks>
        /// <exception cref="CommandLineException">Thrown if at the end of the argument string
        /// or the next argument is an option.</exception>
        public int ReadNextValueInt()
        {
            MoveNextValue();
            return CurrentInt;
        }

        /// <summary>
        /// Throws a <see cref="CommandLineException"/> with the specified error message.
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <remarks>
        /// The message is prefixed with "Command Line Error: "
        /// </remarks>
        /*[DoesNotReturn] Requires .Net 5.0_ or .Net Core 3.0+ */
        public void ThrowError(string message)
        {
            throw new CommandLineException("Command Line Error: " + message);
        }

        /// <summary>
        /// Throws a <see cref="CommandLineException"/> regarding the <see cref="LatestOption"/>
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <remarks>The error message is prefixed with "Command Line Error on Option" with the
        /// option name if CurrentOption has a value. Otherwise prefixed with "Command Line Error"
        /// </remarks>
        /*[DoesNotReturn] Requires .Net 5.0_ or .Net Core 3.0+ */
        public void ThrowValueError(string message)
        {
            throw new CommandLineException(OptionErrPrefix + message);
        }

        /// <summary>
        /// Throws an error indicating that the current argument was unexpected.
        /// </summary>
        /*[DoesNotReturn]  Requires .Net 5.0_ or .Net Core 3.0+ */
        public void ThrowUnexpectedArgError()
        {
            throw new CommandLineException("Command Line Error: Unexpected argument: " + Current);
        }

        private string OptionErrPrefix
        {
            get
            {
                return (string.IsNullOrEmpty(m_latestOption))
                    ? "Command Line Error: "
                    : $"Command Line Error on Option \"{m_latestOption}\": ";
            }
        }

    }

    /// <summary>
    /// An error found when parsing the command line.
    /// </summary>
    /// <remarks>
    /// The <see cref="Message"/> value is always suitable for reporting to the user.
    /// </remarks>
    class CommandLineException : Exception
    {
        public CommandLineException(string errorMessage)
            : base(errorMessage)
        {
        }

    }
}
