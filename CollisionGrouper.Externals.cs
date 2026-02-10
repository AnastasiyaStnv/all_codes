using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Plugins;
using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api.DocumentParts;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using Autodesk.Navisworks.Api.Interop;
using System.Windows;
using CollisionGrouperPlugin.Views;
using CollisionGrouperPlugin.Models;
using System.ComponentModel;



namespace CollisionGrouperPlugin
{


    public class CollisionFragmentGrouping
    {

        public static Document doc;
        public static DocumentClash documentClash;
        public static DocumentClashTests clashTests;
        public List<ClashResult> clashresultofttest = new List<ClashResult>();
        private Dictionary<string, ClashResultGroup> GroupsByGridIntersection = new Dictionary<string, ClashResultGroup>();
        private GridSystem activeSystem;
        List<ClashResultGroup> ResultGroups = new List<ClashResultGroup>();
        public List<string> statuses = new List<string> { "Reviewed", "Approved", "Resolved" };

        public CollisionFragmentGrouping()
        {
            doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            activeSystem = doc.Grids.ActiveSystem;
            documentClash = doc.GetClash();
            clashTests = documentClash.TestsData;
 
        }

        public void makeGroups(List<ClashTest> selectedClashTests)
        {
            foreach (ClashTest test in selectedClashTests)
            {
                GroupsByGridIntersection.Clear();
                Execute(test);
            }
        }

        public int Execute(ClashTest selectedClashTest)
        {
            List<ClashResult> clashResofSelectedTest = GetClashResultsFromTest(selectedClashTest, statuses).ToList();
            List<ClashResultGroup> oldResGroup = GetOldResultsGroup(selectedClashTest, statuses).grups.ToList();
            List<ClashResult> oldRes = GetOldResultsGroup(selectedClashTest, statuses).results.ToList();
            ClashTest newTest = selectedClashTest.CreateCopyWithoutChildren() as ClashTest;
            ClashTest backupTest = selectedClashTest.CreateCopy() as ClashTest;
            int i = documentClash.TestsData.Tests.IndexOf(selectedClashTest);
            documentClash.TestsData.TestsReplaceWithCopy(i, newTest);


            if (selectedClashTest != null)
            {
                foreach (ClashResult theResult in clashResofSelectedTest)
                {
                    if (theResult != null)
                    {
                        GroupResult(theResult);
                    }
                    else
                    {
                        MessageBox.Show("нет результатов теста");
                    }
                }

                ResultGroups = GroupsByGridIntersection.Values.ToList();
                foreach (ClashResultGroup theGroup in ResultGroups)
                {
                    documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[i], theGroup);
                }
                foreach (ClashResultGroup theGroup in oldResGroup)
                {
                    documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[i], theGroup);
                }
                foreach (ClashResult theGroup in oldRes)
                {
                    documentClash.TestsData.TestsAddCopy((GroupItem)documentClash.TestsData.Tests[i], theGroup);
                }
            }
            else
            {
                MessageBox.Show("не нашел группу");
            }
            return 0;
        }

        public void GroupResult(ClashResult theResult)
        {

            theResult = (ClashResult)theResult.CreateCopy();
            theResult.Guid = Guid.Empty;
            ClashResultGroup theGroup;
            GridSystem theGrids = doc.Grids.ActiveSystem;
            GridIntersection theIntersection = theGrids.ClosestIntersection(theResult.Center);
            string fragment = theIntersection.DisplayName.Split('*')[0];
            if (!GroupsByGridIntersection.TryGetValue(fragment, out theGroup))
            {
                theGroup = new ClashResultGroup();
                theGroup.DisplayName = fragment;
                GroupsByGridIntersection.Add(fragment, theGroup);
            }

            theGroup.Children.Add(theResult);

        }
        //Метод получение списка клеш тестов
        public IEnumerable<ClashTest> GetClashTests()
        {
            var clashTests = new List<ClashTest>();
            if (doc != null)
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

        //Метод получение списка несгруппированных клешей выбранного клеш теста
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

        //Метод получения оси пересечения
        private string GetAxisForClash(ClashResult clashResult)
        {
            //Document doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            ClashResult copy = clashResult.CreateCopy() as ClashResult;
            GridIntersection key = activeSystem.ClosestIntersection(copy.Center);
            string axisName = key.DisplayName;
            return axisName;
        }
      
        public resultates GetOldResultsGroup(ClashTest clashTest, List<string> statuses)
        {
            resultates clashResults = new resultates();
            foreach (ClashResultGroup result in clashTest.Children.OfType<ClashResultGroup>())
            {
                ClashResultGroup newresult = new ClashResultGroup();
                newresult = (ClashResultGroup)result.CreateCopy();
                newresult.Guid = Guid.Empty;
                clashResults.grups.Add(newresult);
            }
            foreach (ClashResult result in clashTest.Children.OfType<ClashResult>())
            {
                if (statuses.Contains(result.Status.ToString()))
                {
                    ClashResult newres = new ClashResult();
                    newres = (ClashResult)result.CreateCopy();
                    newres.Guid = Guid.Empty;
                    clashResults.results.Add(newres);
                }
            }
            return clashResults;
        }

    }

}

