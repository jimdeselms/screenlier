import Config from '../config';
import TestRunDetails from '../models/TestRunDetails';
import TestRunSummary from '../models/TestRunSummary';
import * as types from './actionTypes';

import axios from 'axios';

const API_BASE_URI = Config.getApiBaseUri();

export function receiveTestRunSummaries(summaries: TestRunSummary[]) {
    return { 
        type: types.UPDATE_TEST_RUN_SUMMARIES, 
        summaries 
    };
}

export function fetchTestRunSummaries(appname: string) {
    return async (dispatch: any) => {
        let url = `${API_BASE_URI}/testrun`;

        if (appname) {
            url += `?appname=${appname}`;
        }
        
        const response = await axios.get(url);

        const summaries = response.data as TestRunSummary[];

        dispatch(receiveTestRunSummaries(summaries));
    }
}

export function receiveTestRunDetails(testRunDetails: TestRunDetails) {
    return { 
        type: types.UPDATE_TEST_RUN_DETAILS, 
        testRunDetails: {
            ...testRunDetails, 
            isVisible: true
        }
    };
}

export function fetchTestRunDetails(testRunId: number) {
    return async (dispatch: any) => {
        const url = `${API_BASE_URI}/testrun/${testRunId}`;
        const response = await axios.get(url);

        const testRunDetails = response.data as TestRunDetails;

        dispatch(receiveTestRunDetails(testRunDetails));
    }
}

export function promoteImageToBenchmark(testRunId: number, path: string) {
    return async (dispatch: any) => {
        const url = `${API_BASE_URI}/testimage/promote/${testRunId}/${path}`;
        await axios.put(url);

        dispatch(fetchTestRunDetails(testRunId));
    }
}
