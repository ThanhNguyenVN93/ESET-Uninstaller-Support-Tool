using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using frmesetunins;

namespace frmesetunins.Tests
{
    [TestClass]
    public class LogicTests
    {
        [TestMethod]
        public void ParseInt_parses_valid_or_returns_zero()
        {
            Assert.AreEqual(5, Program.ParseInt("5"));
            Assert.AreEqual(0, Program.ParseInt("abc"));
            Assert.AreEqual(0, Program.ParseInt(null));
            Assert.AreEqual(0, Program.ParseInt(""));
        }

        [TestMethod]
        public void GetArgValue_reads_prefixed_value()
        {
            var args = new[] { "--auto", "--password=secret", "--select" };
            Assert.AreEqual("secret", Program.GetArgValue(args, "--password="));
            Assert.IsNull(Program.GetArgValue(args, "--missing="));
            Assert.IsNull(Program.GetArgValue(new string[0], "--password="));
        }

        [TestMethod]
        public void Loc_both_languages_have_identical_keys()
        {
            var vi = Loc.Keys(Loc.Lang.Vi).ToList();
            var en = Loc.Keys(Loc.Lang.En).ToList();
            CollectionAssert.AreEquivalent(vi, en,
                "The Vietnamese and English string tables must define the same keys.");
        }

        [TestMethod]
        public void Loc_no_value_is_empty()
        {
            foreach (var lang in new[] { Loc.Lang.Vi, Loc.Lang.En })
            {
                Loc.Current = lang;
                foreach (var key in Loc.Keys(lang))
                    Assert.IsFalse(string.IsNullOrWhiteSpace(Loc.T(key)),
                        "Empty value for key '" + key + "' in " + lang);
            }
        }

        [TestMethod]
        public void Loc_T_falls_back_to_the_key()
        {
            Assert.AreEqual("__does_not_exist__", Loc.T("__does_not_exist__"));
        }
    }
}
