﻿using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static IPA.Logging.Logger;

namespace IPA.Injector
{
    internal static class Updates
    {
        private const string DeleteFileName = Updating.BeatMods.Updater.SpecialDeletionsFile;
        public static void InstallPendingUpdates()
        {
            var pendingDir = Path.Combine(BeatSaber.InstallPath, "IPA", "Pending");
            if (!Directory.Exists(pendingDir)) return; 
            
            // there are pending updates, install
            updater.Info("Installing pending updates");

            var toDelete = new string[0];
            var delFn = Path.Combine(pendingDir, DeleteFileName);
            if (File.Exists(delFn))
            {
                toDelete = File.ReadAllLines(delFn);
                File.Delete(delFn);
            }

            foreach (var file in toDelete)
            {
                try
                {
                    File.Delete(Path.Combine(BeatSaber.InstallPath, file));
                }
                catch (Exception e)
                {
                    updater.Error("While trying to install pending updates: Error deleting file marked for deletion");
                    updater.Error(e);
                }
            }

            #region Self Protection

            string path;
            if (Directory.Exists(path = Path.Combine(pendingDir, "IPA")))
            {
                var dirs = new Stack<string>(20);
                
                dirs.Push(path);

                while (dirs.Count > 0)
                {
                    var currentDir = dirs.Pop();
                    string[] subDirs;
                    string[] files;
                    try
                    {
                        subDirs = Directory.GetDirectories(currentDir);
                        files = Directory.GetFiles(currentDir);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        updater.Error(e);
                        continue;
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        updater.Error(e);
                        continue;
                    }

                    foreach (var file in files)
                    {
                        try
                        {
                            if (!Utils.GetRelativePath(file, path).Split(Path.PathSeparator).Contains("Pending"))
                                File.Delete(file);
                        }
                        catch (FileNotFoundException e)
                        {
                            updater.Error(e);
                        }
                    }
                    
                    foreach (var str in subDirs)
                        dirs.Push(str);
                }
            }
            if (File.Exists(path = Path.Combine(pendingDir, "IPA.exe")))
            {
                File.Delete(path);
                if (File.Exists(path = Path.Combine(pendingDir, "Mono.Cecil.dll")))
                    File.Delete(path);
            }

            #endregion

            try
            {
                Utils.CopyAll(new DirectoryInfo(pendingDir), new DirectoryInfo(BeatSaber.InstallPath), onCopyException: (e, f) =>
                {
                    updater.Error($"Error copying file {Utils.GetRelativePath(f.FullName, pendingDir)} from Pending:");
                    updater.Error(e);
                    return true;
                });
            }
            catch (Exception e)
            {
                updater.Error("While trying to install pending updates: Error copying files in");
                updater.Error(e);
            }

            try
            {
                Directory.Delete(pendingDir, true);
            }
            catch (Exception e)
            {
                updater.Error("Something went wrong performing an operation that should never fail!");
                updater.Error(e);
            }
        }
    }
}
