//  Copyright (c) 2015, Michael unfried
//  Email:  serbius3@gmail.com
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  Redistributions of source code must retain the above copyright notice, 
//  this list of conditions and the following disclaimer. 
//  Redistributions in binary form must reproduce the above copyright notice, 
//  this list of conditions and the following disclaimer in the documentation 
//  and/or other materials provided with the distribution. 

//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SpirographUI
{
    static class EntryPoint
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Application Started");
#endif
            string argPrefix;
            int argHandle;
            if (args.Length > 2)
            {
                System.Diagnostics.Debug.WriteLine("Too many arguments on the command line.");
                return;
            }
            ParseArgsToPrefixAndArgInt(args, out argPrefix, out argHandle);

            try
            {
                switch (argPrefix)
                {
                    case "/a":      // Password dialog requested.
                        //Win32.LockWorkstationApi();
                        break;
                    case "/c":      // Show the config dialog.
                        using (frmConfig frm = new frmConfig())
                            frm.ShowDialog(Form.FromHandle((IntPtr)argHandle));
                        break;
                    case "/p":      // Create mini-preview on Display Properties dialog.
                        if (argHandle == 0) goto case "/s"; // No handle found, do a full screen saver.
                        else
                            using (SpirographUI mpTemp = new SpirographUI())
                                mpTemp.RunPreview((IntPtr)argHandle);
                        break;
                    case "/s":
                        using (SpirographUI ssClass = new SpirographUI())
                            ssClass.RunFullScreen();
                        break;
                    case "/w":
                    default:
                        // We're launching this in "Forms" mode, meaning we're just rendering to a standard Windows form control.
                        using (SpirographUI spiro = new SpirographUI())
                        using (frmSpiro frm = new frmSpiro())
                        {
                            frm.Shown += delegate
                            {
                                spiro.RunFormsMode(frm);
                            };
                            Application.Run(frm);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Fatal error: " + ex.ToString());
#endif
                // If an error occurs in Release mode, just exit.
            }
            Application.Exit();
        }
        public static void OnExcept(object sender, UnhandledExceptionEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(args.ExceptionObject.ToString());
            Application.Exit();
        }
        private static void ParseArgsToPrefixAndArgInt(string[] args, out string argPrefix, out int argHandle)
        {
            string curArg;
            char[] SpacesOrColons = { ' ', ':' };

            // This is really probably not the best way to do this.  I imported this code from a 10+-year-old version of this project, because it works.
            switch (args.Length)
            {
                case 0: // Nothing on command line, so just start the screensaver.
                    argPrefix = "/w";
                    argHandle = 0;
                    break;
                case 1:
                    curArg = args[0];
                    argPrefix = curArg.Substring(0, 2);
                    curArg = curArg.Replace(argPrefix, ""); // Drop the slash /? part.
                    curArg = curArg.Trim(SpacesOrColons); // Remove colons and spaces.
                    argHandle = curArg == "" ? 0 : int.Parse(curArg); // if empty return zero. else get handle.
                    break;
                case 2:
                    argPrefix = args[0].Substring(0, 2);
                    argHandle = int.Parse(args[1].ToString());
                    break;
                default:
                    argHandle = 0;
                    argPrefix = "";
                    break;
            }
        }
    }
}
