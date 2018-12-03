import * as types from '../actions/actionTypes';
import initialState from './initialState';

export default function ids(state = initialState.testRunDetails, action: any) {
    switch (action.type) {
        case types.UPDATE_TEST_RUN_DETAILS:
            return action.testRunDetails;
        case types.UPDATE_TEST_RUN_SUMMARIES:
            return {
                isVisible: false,
                testRunId: 1,
                name: "",
                application: "",
                testImages: [],
                referenceImages: []
            };
        default:
            return state;
    }
}