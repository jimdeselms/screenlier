import { combineReducers } from 'redux';
import summaries from './summariesReducer';
import testRunDetails from './testRunDetailsReducer';


const rootReducer = combineReducers({ testRunDetails, summaries });

export default rootReducer;