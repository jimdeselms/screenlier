import * as React from 'react';
import { connect } from 'react-redux';
import { Link } from 'react-router-dom';
import ProgressBar from './ProgressBar';

export interface SummaryProps {
    testRunId: number,
    application: string,
    start: string,
    end?: string,
    expectedTestCount?: number,
    testCount: number,
    successCount: number,
    missingBenchmarkCount: number,
    differenceCount: number,
    errorCount: number        
}

class Summary extends React.Component<SummaryProps> {
    constructor(props: SummaryProps) {
        super(props);
    }

    public render() {
        let progressBarState: string;
        if (this.props.errorCount > 0 || this.props.differenceCount > 0) {
            progressBarState = 'error';
        } else if (this.props.missingBenchmarkCount > 0) {
            progressBarState = 'warn';
        } else if (this.props.successCount < this.props.testCount) {
            progressBarState = 'wait';
        } else {
            progressBarState = 'success';
        }
        
        return (
            <p>
                <Link to={'/test/' + this.props.testRunId}>{this.props.testRunId}</Link> {this.props.start} - {this.getTestProgressState()}
                <ProgressBar currValue={this.props.testCount} maxValue={this.props.expectedTestCount} state={progressBarState} />
            </p>
        );
    }

    public componentDidMount() {
        //
    }

    private getTestProgressState(): string {
        return (this.props.end ? 'Finished' : 'Running');
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

export default connect(mapStateToProps, mapDispatchToProps)(Summary);