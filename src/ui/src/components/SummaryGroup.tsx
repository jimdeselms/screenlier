import * as _ from 'lodash';
import * as React from 'react';
import { connect } from 'react-redux';
import Summary from './Summary';
import { SummaryProps } from './Summary';

import './Summaries.css';

interface SummaryGroupProps {
    application: string,
    summaries: SummaryProps[],
    expectedTestCount: number
}

class SummaryGroup extends React.Component<SummaryGroupProps> {
    constructor(props: SummaryGroupProps) {
        super(props);
    }

    public render() {

        const summaryItems = _.chain(this.props.summaries)
            .map(s => <Summary 
                key={`${s.application}|${s.testRunId}`}
                application={s.application} 
                testRunId={s.testRunId} 
                start={s.start}
                end={s.end}
                expectedTestCount={s.end ? s.testCount : this.props.expectedTestCount}
                testCount={s.testCount}
                successCount={s.successCount}
                missingBenchmarkCount={s.missingBenchmarkCount}
                differenceCount={s.differenceCount}
                errorCount={s.errorCount} />)
            .value();

        return (
            <div className="App">
                <b>{this.props.application}</b>
                <ul>
                    { summaryItems }
                </ul>
            </div>
        );
    }
}

function mapStateToProps(state: any) {
    return{
    };
}

function mapDispatchToProps(dispatch: any) {
    return {
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(SummaryGroup);