#tool "xunit.runner.console&version=2.4.1"
#load "Configuration.cake"

Task("Test:XUnit2")    
    .IsDependentOn("Build")
    .IsDependeeOf("Test")
    .WithCriteria<Configuration>((ctx, config) => config.Solution.TestProjects.Any(p => p.IsXUnitTestProject()))
    .Does<Configuration>(config => 
{
    CreateDirectory($"{config.Artifacts.Root}/test-results");

    var shouldFail = false;
    foreach(var testProject in config.Solution.TestProjects.Where(p => p.IsXUnitTestProject())) {
        var assemblyName = config.Solution.GetProjectName(testProject);
        var testResultsRoot = $"{config.Artifacts.Root}/test-results";
        var testResultsXml = $"{testResultsRoot}/{assemblyName}.xml";
        var testAssembly = $"{testProject.OutputPaths.FirstOrDefault().ToString()}/{assemblyName}.dll";

        try 
        {
            var settings = new XUnit2Settings {
                XmlReport = true,
                ReportName = assemblyName,
                OutputDirectory = $"{testResultsRoot}",
            };
            
            XUnit2(testAssembly, settings);
        } 
        catch
        {
            shouldFail = true;
        }
    }

    Information("Publishing Test results from {0}", config.Artifacts.Root);
    var testResults = GetFiles($"{config.Artifacts.Root}/test-results/**/*.xml").ToArray();
    if(testResults.Any()) 
    {
        if(BuildSystem.IsRunningOnAzurePipelinesHosted || AzurePipelines.IsRunningOnAzurePipelines) 
        {
            AzurePipelines.Commands.PublishTestResults(new AzurePipelinesPublishTestResultsData() {
                Configuration = config.Solution.BuildConfiguration,
                MergeTestResults = true,
                TestResultsFiles = testResults,
                TestRunner = AzurePipelinesTestRunnerType.VSTest
            });    
        }
    }

    if(shouldFail)
    {
        throw new Exception("Tests have failed");
    }
});
