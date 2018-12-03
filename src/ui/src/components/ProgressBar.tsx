import * as _ from 'lodash';
import * as React from 'react';
import { connect } from 'react-redux';

import './ProgressBar.css';

interface ProgressBarProps {
    maxValue?: number,
    currValue: number,
    state: string
}

class ProgressBar extends React.Component<ProgressBarProps> {
    constructor(props: ProgressBarProps) {
        super(props);
    }

    public render() {
        let stateClass;
        switch (this.props.state) {
            case 'success': stateClass = 'progress-bar-success'; break;
            case 'warn': stateClass = 'progress-bar-warn'; break;
            case 'error': stateClass = 'progress-bar-error'; break;
            case 'wait': stateClass = 'progress-bar-wait'; break;
        }

        const rawWidth = (this.props.currValue / (this.props.maxValue || this.props.currValue)) * 100.0;
        const width = rawWidth > 100 ? 100 : rawWidth;
        
        const noMaxClass = this.props.maxValue ? '' : 'progress-bar-nomax';

        const className = `progress-bar-progress ${stateClass} ${noMaxClass}`;

        return (
            <div className='progress-bar-container'>
                    <div className={ className } style={ {width: `${width}%`} }/>
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

export default connect(mapStateToProps, mapDispatchToProps)(ProgressBar);