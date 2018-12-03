export default interface TestImageModel {
    testRunId: string,
    path: string,
    name: string,
    state: string,
    compareStart: string,
    error?: string
}
