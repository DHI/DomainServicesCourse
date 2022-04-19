namespace Workflows.Test;

using Xunit.Abstractions;
using Xunit.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

public class AlphabeticalOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        var result = testCases.ToList();
        result.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
        return result;
    }
}