﻿//  Copyright 2015 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using NDesk.Options;
using NtApiDotNet;
using System;
using System.IO;
using System.Windows.Forms;

namespace EditSection
{
    static class Program
    {
        static string GetName(NtSection section, NtMappedSection map)
        {
            string name = String.Empty;
            try
            {
                name = map.FullPath;
                if (string.IsNullOrEmpty(name))
                {
                    name = section.FullPath;
                }
            }
            catch (NtException)
            {
            }

            return string.IsNullOrEmpty(name) ? 
                    $"Handle {section.Handle.DangerousGetHandle()} - 0x{map.DangerousGetHandle().ToInt64():X}" : name;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            NtToken.EnableDebugPrivilege();

            try
            {
                if (args.Length == 0)
                {
                    Application.Run(new MainForm());
                }
                else
                {
                    int handle = -1;
                    string text = String.Empty;
                    bool read_only = false;
                    string filename = string.Empty;

                    OptionSet opts = new OptionSet() {
                        { "handle=", "Specify an inherited handle to view.",
                            v => handle = int.Parse(v) },
                        { "readonly", "Specify view section readonly", v => read_only = v != null },
                        { "file=", "Specify a file to view", v => filename = v },
                    };

                    opts.Parse(args);

                    if (handle > 0)
                    {
                        using (var section = NtSection.FromHandle(new SafeKernelObjectHandle(new IntPtr(handle), true)))
                        {
                            read_only = read_only || !section.IsAccessGranted(SectionAccessRights.MapWrite);
                            using (var map = read_only ? section.MapRead() : section.MapReadWrite())
                            {
                                using (SectionEditorForm frm = new SectionEditorForm(map, GetName(section, map), read_only))
                                {
                                    Application.Run(frm);
                                }
                            }
                        }
                    }
                    else if (File.Exists(filename))
                    {
                        using (var file = NtFile.Open(NtFileUtils.DosFileNameToNt(filename), null, 
                            FileAccessRights.ReadData, FileShareMode.Read | FileShareMode.Delete, FileOpenOptions.NonDirectoryFile))
                        {
                            using (NtSection section = NtSection.CreateReadOnlyDataSection(file))
                            {
                                using (var map = section.MapRead())
                                {
                                    using (SectionEditorForm frm = new SectionEditorForm(map, filename, true, file.Length))
                                    {
                                        Application.Run(frm);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Invalid command line arguments");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
