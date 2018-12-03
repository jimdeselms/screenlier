import * as _ from 'lodash';
import * as React from 'react';
import { connect } from 'react-redux';
import { fetchTestRunSummaries } from '../actions/actions';
import { SummaryProps } from './Summary';
import SummaryGroup from './SummaryGroup';

import './Summaries.css';

interface HomeProps {
    id: string,
    match: { params: { id: string, appname: string } },
    summaries: SummaryProps[],
    fetchTestRunSummaries: (appname: string) => void;
}

class Summaries extends React.Component<HomeProps> {
    constructor(props: HomeProps) {
        super(props);
    }

    public render() {

        // The expected count is the average of the last three runs that have completed.
        const expectedCounts = _.chain(this.props.summaries)
            .filter(s => s.end !== undefined)
            .groupBy('application')
            .mapValues(summaries => _.chain(summaries).take(3).value())
            .mapValues(summaries => summaries.length > 0
                ? _.chain(summaries).map(s => s.testCount).sum().value() / summaries.length
                : undefined)
            .value();

        const summaryDict = _.chain(this.props.summaries)
            .orderBy(s => s.testRunId, 'desc')
            .groupBy(s => s.application)
            .value();

        const summaryGroups: any[] = [];
        Object.keys(summaryDict).forEach(application => {
            const summaries = summaryDict[application];

            summaryGroups.push(<SummaryGroup
                application={application}
                summaries={summaries}
                expectedTestCount={expectedCounts[application] || 0} />);
        });

        return (
            <div className="App">
                <ul>
                    { summaryGroups }
                </ul>
            </div>
        );
    }

    private intervalId: any;

    public componentDidMount() {
        this.props.fetchTestRunSummaries(this.props.match.params.appname);

        this.intervalId = setInterval(() => this.props.fetchTestRunSummaries(this.props.match.params.appname), 5000);
    }

    public componentWillUnmount() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
        }
    }
}

function mapStateToProps(state: any) {
    return{
        summaries: state.summaries
    };
}

function mapDispatchToProps(dispatch: any) {
    return {
        fetchTestRunSummaries: (appname: string) => dispatch(fetchTestRunSummaries(appname))
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(Summaries);