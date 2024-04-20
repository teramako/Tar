using teramako.IO.Tar;

namespace test
{
    [TestClass]
    public class UnitTest1
    {
        static private string testTarFile = @"D:\proj\Tar\test\test.tar";

        private List<TarEntry> entryList = new List<TarEntry>();

        public UnitTest1()
        {
            var file = new FileInfo(testTarFile);
            using (var fs = file.OpenRead())
            using (var tarArchive = new TarArchiveReader(fs))
            {
                foreach (var entry in tarArchive.GetEntries())
                {
                    entryList.Add(entry);
                }
            }

        }
        [TestMethod]
        public void TestMethod1()
        {
            foreach (var entry in entryList)
            {
                Console.WriteLine(entry.Header.Type);
                Console.WriteLine(entry.ToString());
                Assert.IsFalse(entry.CanRead);
            }
        }
        [TestMethod]
        public void TestMethod2()
        {
            foreach (var entry in  entryList) {
                var name = entry.Header.Name;
                switch (name)
                {
                    case "testdir/":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Directory));
                        Assert.AreEqual("0755", entry.Header.Permission.Octet); 
                        Assert.AreEqual(0, entry.Header.Size);
                        Assert.AreEqual("ustar", entry.Header.Magic);
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "testdir/test_file_1.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("0644", entry.Header.Permission.Octet);
                        Assert.AreEqual("rw-r--r--", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "perm_test/perm_0000.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("0000", entry.Header.Permission.Octet);
                        Assert.AreEqual("---------", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "perm_test/perm_4000.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("4000", entry.Header.Permission.Octet);
                        Assert.AreEqual("--S------", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "perm_test/perm_2000.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("2000", entry.Header.Permission.Octet);
                        Assert.AreEqual("-----S---", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "perm_test/perm_1000.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("1000", entry.Header.Permission.Octet);
                        Assert.AreEqual("--------T", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "perm_test/perm_4100.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("4100", entry.Header.Permission.Octet);
                        Assert.AreEqual("--s------", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "perm_test/perm_2010.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("2010", entry.Header.Permission.Octet);
                        Assert.AreEqual("-----s---", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "perm_test/perm_1001.txt":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.AreEqual("1001", entry.Header.Permission.Octet);
                        Assert.AreEqual("--------t", entry.Header.Permission.ToString());
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "longname_1_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.IsFalse(entry.Header.Type.HasFlag(TarEntryType.GNU_LongName));
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "longname_2_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_GNU_longname":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.Regular));
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.GNU_LongName));
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "link_1":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.SymbolicLink));
                        Assert.IsFalse(entry.Header.Type.HasFlag(TarEntryType.GNU_LongLink));
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "longlink_2":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.SymbolicLink));
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.GNU_LongLink));
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    case "longlink_3_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_GNU_longlink":
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.SymbolicLink));
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.GNU_LongName));
                        Assert.IsTrue(entry.Header.Type.HasFlag(TarEntryType.GNU_LongLink));
                        Console.WriteLine("OK ... :{0}", name);
                        break;
                    default:
                        Console.WriteLine("Not Tested: {0}", name);
                        break;
                }
            }
        }
        [TestMethod]
        public void Test3()
        {
            var tarArchive = new TarArchiveReader(testTarFile);
            foreach (var entry in tarArchive.GetEntries())
            {
                Console.WriteLine(entry.Header.Name);
                if (entry.Header.Size > 0)
                {
                    using (var sr = new StreamReader(entry))
                    {
                        Console.WriteLine(sr.ReadToEnd());
                    }
                }
            }
        }
        [TestMethod]
        public void Test4()
        {
            Assert.ThrowsException<FileNotFoundException>(() =>
            {
                new TarArchiveReader("notFoundPath");
            });
        }
    }
}