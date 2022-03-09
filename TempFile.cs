/*
---
name: TempFile.cs
description: CodeBit class that manages the lifetime of a temporary file.
url: https://raw.githubusercontent.com/bredd/TempFile/main/TempFile.cs
version: 1.0
keywords: CodeBit
dateModified: 2021-06-18
license: http://unlicense.org
# Metadata in MicroYaml format. See http://filemeta.org/CodeBit.html
...
*/

/*
=== Unlicense ===
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or distribute this software,
either in source code form or as a compiled binary, for any purpose, commercial or
non-commercial, and by any means.

In jurisdictions that recognize copyright laws, the author or authors of this software
dedicate any and all copyright interest in the software to the public domain. We make
this dedication for the benefit of the public at large and to the detriment of our heirs
and successors. We intend this dedication to be an overt act of relinquishment in
perpetuity of all present and future rights to this software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to http://unlicense.org
*/

using System;
using System.Diagnostics;
using System.IO;

namespace CodeBit.IO
{
    /// <summary>
    /// Represents a temporary file in the user's temp file directory
    /// that uses IDisposable, backed by a finalizer, to ensure the
    /// file is deleted when you are finished with it.
    /// </summary>
    class TempFile : IDisposable
    {
        string m_fullname;

        /// <summary>
        /// Create a zero byte temp file with lifetime managed by this object.
        /// </summary>
        public TempFile()
        {
            m_fullname = Path.GetTempFileName();
        }

        /// <summary>
        /// Create a zero byte temp file in the temp directory but with the specified filename
        /// </summary>
        /// <param name="filename">Name of the file, in the temp directory, to create.</param>
        /// <remarks>
        /// If there is an existing file by this name it will be overwritten.
        /// </remarks>
        public TempFile(string filename)
        {
            m_fullname = Path.Combine(Path.GetTempPath(), filename);
            new FileStream(m_fullname, FileMode.Create, FileAccess.ReadWrite, FileShare.None).Dispose();
        }

        /// <summary>
        /// Get the full name of the temp file.
        /// </summary>
        public string FullName { get { return m_fullname; } }

        /// <summary>
        /// Open the temporary file.
        /// </summary>
        /// <param name="fileAccess"><see cref="FileAccess"/> value. Defaults to ReadWrite.</param>
        /// <param name="fileShare"><see cref="FileShare"/> value. Defaults to None.</param>
        /// <returns>A <see cref="Stream"/></returns>
        public FileStream Open(FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.None)
        {
            return new FileStream(m_fullname, FileMode.Open, fileAccess, fileShare);
        }

        /// <summary>
        /// Deletes the temporary file (any open handles must be closed first).
        /// </summary>
        public void Dispose()
        {
            if (m_fullname != null)
            {
                File.Delete(m_fullname);
                m_fullname = null;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Ensures the temporary file gets deleted.
        /// </summary>
        ~TempFile()
        {
            if (m_fullname != null)
            {
                Debug.Write("Failed to dispose TempFile.");
                File.Delete(m_fullname);
                m_fullname = null;
            }
        }
    }
}
