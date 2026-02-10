using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api.Interop;
using Autodesk.Navisworks.Api.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using System.Windows;
using System.Diagnostics;

namespace CollisionClusterPlugin
{
    public class Clustering
    {
        public Document ActiveDocument { get; }
        public static Document oDoc = Autodesk.Navisworks.Api.Application.ActiveDocument;
        public static DocumentClash documentClash = oDoc.GetClash();
        private ClashResultGroup _selectedGroup;
        public List<string> statuses = new List<string> { "Reviewed", "Approved", "Resolved" };

        public int MakeClusters()
        {
            LcClCurrentIssue instance = LcClCurrentIssue.GetInstance((LcOpState)oDoc.State);
            ClashTest currentTest = instance.GetCurrentTest();
            SavedItem issueAsSavedItem = LcClCurrentIssue.GetInstance((LcOpState)Autodesk.Navisworks.Api.Application.ActiveDocument.State).GetCurrentIssueAsSavedItem();
            List<List<string>> clashofGroup = new List<List<string>>();
            this._selectedGroup = issueAsSavedItem as ClashResultGroup;
            Guid currentGuid = _selectedGroup.Guid;
            ClashResultGroup selectGroup = GetClashResofCurrentGroup(currentGuid);
            List<ClashResultGroup> newGroups = new List<ClashResultGroup>();
            ClashTest testOfGroup = selectGroup.Parent as ClashTest;
            if (selectGroup != null)
            {
                foreach (ClashResult children in selectGroup.Children)
                {
                    List<string> clashItems = new List<string>();
                    if (children.CompositeItem1 != null && children.CompositeItem2 != null)
                    {
                        ModelItem modIt1 = children.CompositeItem1;
                        ModelItem modIt2 = children.CompositeItem2;

                        string el1Id = GetElementId(modIt1);
                        string el2Id = GetElementId(modIt2);
                        string el1FileName = modIt1.PropertyCategories.FindPropertyByDisplayName("Элемент", "Файл источника").ToString().Split(':').Last();
                        string el2FileName = modIt2.PropertyCategories.FindPropertyByDisplayName("Элемент", "Файл источника").ToString().Split(':').Last();
                        string el1 = el1Id + "_" + el1FileName;
                        string el2 = el2Id + "_" + el2FileName;
                        clashItems.Add(el1);
                        clashItems.Add(el2);
                        clashofGroup.Add(clashItems);
                    }
                }
                List<List<int>> grouped = UnionPair.GroupPairsIndices(clashofGroup);
                newGroups = GetNewCurrentGroup(grouped, selectGroup);
               
            }
            else
            {
                MessageBox.Show("это не группа");
            }
            List<ClashResult> oldRes = GetOldResultsGroup(testOfGroup, currentGuid).results.ToList();
            List<ClashResultGroup> oldResGroup = GetOldResultsGroup(testOfGroup, currentGuid).grups.ToList();
            ClashTest newTest = testOfGroup.CreateCopyWithoutChildren() as ClashTest;
            int i = documentClash.TestsData.Tests.IndexOf(testOfGroup);
            documentClash.TestsData.TestsReplaceWithCopy(i, newTest);

            foreach (ClashResultGroup theGroup in oldResGroup)
            {
                documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[i], theGroup);
            }
            foreach (ClashResult theGroup in oldRes)
            {
                documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[i], theGroup);
            }
            foreach (ClashResultGroup theGroup in newGroups)
            {
                documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[i], theGroup);
            }
            return 0;
        }

        public IEnumerable<ClashTest> GetClashTests()
        {
            var clashTests = new List<ClashTest>();
            if (oDoc != null)
            {
                //получаем коллекцию клеш тестов
                var clashTestCollection = documentClash.TestsData.Tests;

                //перебираем и добавляем их в список
                foreach (var test in clashTestCollection)
                {
                    if (test is ClashTest clashTest)
                    {
                        clashTests.Add(clashTest);
                    }
                }

            }
            return clashTests;
        }

        //Метод получения ID Элемента
        private string GetElementId(ModelItem modelItem)
        {
            DataProperty propertyByDisplayName = modelItem.PropertyCategories.FindPropertyByDisplayName("Объект", "Id");
            if ((NativeHandle)propertyByDisplayName != (NativeHandle)null)
                return propertyByDisplayName.Value.ToInt32().ToString();
            return (NativeHandle)modelItem.Parent != (NativeHandle)null ? this.GetElementId(modelItem.Parent) : (string)null;
        }

        public IEnumerable<ClashResult> GetClashResultsFromTest(ClashTest clashTest, List<string> statuses)
        {
            List<ClashResult> clashResults = new List<ClashResult>();

            foreach (ClashResult result in clashTest.Children.OfType<ClashResult>())
            {
                if (!statuses.Contains(result.Status.ToString()))
                {
                    clashResults.Add(result);
                }
            }
            return clashResults;
        }

        public resultates GetOldResultsGroup(ClashTest clashTest,Guid currentGuid)
        {
            resultates clashResults = new resultates();
            foreach (ClashResultGroup result in clashTest.Children.OfType<ClashResultGroup>())
            {
                if (result.Guid != currentGuid)
                {
                    ClashResultGroup newresult = new ClashResultGroup();
                    newresult = (ClashResultGroup)result.CreateCopy();
                    newresult.Guid = Guid.Empty;
                    clashResults.grups.Add(newresult);
                }
            }
            foreach (ClashResult result in clashTest.Children.OfType<ClashResult>())
            {

                    ClashResult newres = new ClashResult();
                    newres = (ClashResult)result.CreateCopy();
                    newres.Guid = Guid.Empty;
                    clashResults.results.Add(newres);
            }
            return clashResults;
        }

        public class resultates
        {
            public List<ClashResultGroup> grups { get; set; }
            public List<ClashResult> results { get; set; }
            public resultates()
            {
                grups = new List<ClashResultGroup>();
                results = new List<ClashResult>();
            }

        }
        public List <ClashResultGroup> GetNewCurrentGroup(List<List<int>> grouped, ClashResultGroup selectGroup)
        {
            List<ClashResultGroup> result = new List<ClashResultGroup>();
            ClashResultGroup copyGroup = selectGroup.CreateCopy() as ClashResultGroup;
            foreach (List<int> GroupItem in grouped)
            {
                ClashResultGroup thegroup = new ClashResultGroup();
                thegroup.DisplayName = selectGroup.DisplayName + "_";
                foreach (int poz in GroupItem)
                {
                    ClashResult posit = new ClashResult();
                    posit = copyGroup.Children[poz].CreateCopy() as ClashResult;
                    thegroup.Children.Add(posit);
                }
                result.Add(thegroup);
            }
            return result;
        }
            public ClashResultGroup GetClashResofCurrentGroup(Guid ClashGroupGuid)
            {
            var clashTests = new List<ClashTest>();
            clashTests = GetClashTests().ToList();
            foreach (ClashTest test in clashTests)
            {
                foreach (ClashResultGroup group in test.Children.OfType<ClashResultGroup>())
                {
                    if (group is ClashResultGroup)
                    {
                        if (group.Guid == ClashGroupGuid)
                        {
                            return group;
                        }
                    }
                }
            }
                return null;
            }
    }
}
