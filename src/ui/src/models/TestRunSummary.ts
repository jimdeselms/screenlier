export default interface TestRunSummary {
    testRunId: string,
    application: string,
    start: string,
    end?: string,
    testCount: number,
    successCount: number,
    missingBenchmarkCount: number,
    differenceCount: number,
    errorCount: number        
}

