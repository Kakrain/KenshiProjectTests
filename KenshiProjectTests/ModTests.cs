using KenshiCore.Mods;
using KenshiCore.ReverseEngineering;
using KenshiCore.Utilities;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using ScintillaNET;
using System;
using System.IO;
using Xunit.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace KenshiProjectTests
{
    public class ModTests
    {
        private readonly ITestOutputHelper output; 
        public ModTests(ITestOutputHelper output)
        {
            this.output = output;
            CoreUtils.setAdditionalCallback(s => this.output.WriteLine(s));
        }
        [Fact]
        public void LoadSaveTests()
        {
            //TestModsIn("C:/SteamGames/steamapps/common/Kenshi/mods/Naginata");
            //return;
            foreach (string folder in GetFoldersFrom("C:/SteamGames/steamapps/common/Kenshi/mods/"))
            {
                TestModsIn(folder);
            }
            foreach (string folder in GetFoldersFrom("C:/SteamGames/steamapps/workshop/content/233860"))
            {
                TestModsIn(folder);
            }
        }
        private List<string> GetFoldersFrom(string path)
        {
            return Directory.GetDirectories(path).ToList();
        }

        private void TestModsIn(string path)
        {
            ReverseEngineer re = new ReverseEngineer();
            foreach (string file in Directory.GetFiles(path, "*.mod"))
            {
                ModWriter writer=new ModWriter();
                int ftype = ReverseEngineer.readJustFiletype(file);
                if (ftype==14)// es  v15
                {
                    CoreUtils.Print($"file:{Path.GetFileName(file)} is v{ftype}");
                    continue;
                }
                bool correctly_loaded=re.LoadModFile(file);
                
                Assert.True(correctly_loaded, $"mod incorrectly loaded {file}");
                string resavedPath = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + ".resaved");
                try
                {
                    re.SaveModFile(resavedPath); 
                    bool same = AreTheSame(file, resavedPath);

                    Assert.True(same, $"A mod failed in {file}");
                }
                finally
                {
                    SendFileToRecycleBin(resavedPath);
                }

            }
        }
        private bool AreTheSame(string path1,string path2)
        {
            return File.ReadAllBytes(path1).SequenceEqual(File.ReadAllBytes(path2));
        }
        private bool AreTheSameDetailedRec(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);

            int minLength = Math.Min(file1.Length, file2.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (file1[i] != file2[i])
                {
                    int start = Math.Max(0, i - 16);
                    int end = Math.Min(minLength, i + 16);

                    output.WriteLine(
                        $"First difference at byte {i}: " +
                        $"original=0x{file1[i]:X2}, " +
                        $"resaved=0x{file2[i]:X2}");

                    output.WriteLine(
                        $"Original: {Convert.ToHexString(file1[start..end])}");

                    output.WriteLine(
                        $"Resaved:  {Convert.ToHexString(file2[start..end])}");

                    return false;
                }
            }

            if (file1.Length != file2.Length)
            {
                output.WriteLine(
                    $"Sizes differ: original={file1.Length}, resaved={file2.Length}");
                return false;
            }

            return true;
        }
        private bool AreTheSameDetailed(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);

            output.WriteLine("Original:");
            output.WriteLine(Convert.ToHexString(file1.Take(32).ToArray()));

            output.WriteLine("Resaved:");
            output.WriteLine(Convert.ToHexString(file2.Take(32).ToArray()));

            int minLength = Math.Min(file1.Length, file2.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (file1[i] != file2[i])
                {
                    output.WriteLine(
                        $"First difference at byte {i}: " +
                        $"original=0x{file1[i]:X2}, " +
                        $"resaved=0x{file2[i]:X2}"
                    );

                    return false;
                }
            }

            if (file1.Length != file2.Length)
            {
                output.WriteLine(
                    $"Files match for first {minLength} bytes, " +
                    $"but sizes differ: original={file1.Length}, resaved={file2.Length}"
                );

                return false;
            }

            return true;
        }
        private void SendFileToRecycleBin(string path)
        {
            FileSystem.DeleteFile(path,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin);
        }
        

    }
}
