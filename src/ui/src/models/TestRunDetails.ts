import ReferenceImageModel from './ReferenceImageModel';
import TestImageModel from './TestImageModel';

export default interface TestRunDetails {
    isVisible: false,
    testRunId: string,
    name: string,
    testImages: TestImageModel[],
    referenceImages: ReferenceImageModel[],
    error?: string
}

