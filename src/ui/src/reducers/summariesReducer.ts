import * as types from '../actions/actionTypes';
import initialState from './initialState';

export default function ids(state = initialState.summaries, action: any) {
    switch (action.type) {
        case types.UPDATE_TEST_RUN_DETAILS:
            return [];
        case types.UPDATE_TEST_RUN_SUMMARIES:
            return action.summaries;
        default: 
            return state;
    }
}