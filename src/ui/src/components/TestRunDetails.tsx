import * as _ from 'lodash';
import * as React from 'react';
import { connect } from 'react-redux';
import { fetchTestRunDetails } from '../actions/actions';

import Config from '../config';
import ReferenceImageModel from '../models/ReferenceImageModel';
import TestImageModel from '../models/TestImageModel';
import TestImage from './TestImage';

const IMAGE_BASE_URI = Config.getImageBaseUri();

import './TestRunDetails.css';

export interface TestRunDetailsProps {
    isVisible: boolean,
    match: { params: { id: string } },
    name: string,
    application: string,
    testImages: TestImageModel[],
    referenceImages: ReferenceImageModel[],
    fetchTestRunDetails: any,
    error?: string
}

interface TestRunDetailsState {
    showSuccess: boolean
    showReferenceImages: boolean
}

class TestRunDetails extends React.Component<TestRunDetailsProps, TestRunDetailsState> {
    constructor(props: TestRunDetailsProps) {
        super(props);

        this.state = {
            showSuccess: false,
            showReferenceImages: false,
        }

        this.toggleShowSuccess = this.toggleShowSuccess.bind(this);
        this.toggleShowReferenceImages = this.toggleShowReferenceImages.bind(this);
    }

    public render() {
        if (this.props.isVisible) {
            return (
                <div>
                    <div onClick={this.toggleShowSuccess}><input type='checkbox' checked={this.state.showSuccess}/>Show success</div>
                    <div onClick={this.toggleShowReferenceImages}><input type='checkbox' checked={this.state.showReferenceImages}/>Show reference images</div>
                    <p>{this.props.name}</p>
                    { this.getTestImages() }
                </div>
            );
        } else {
            return null;
        }
    }

    public toggleShowSuccess() {
        this.setState(
            { showSuccess: !this.state.showSuccess }
        );
    }

    public toggleShowReferenceImages() {
        this.setState(
            { showReferenceImages: !this.state.showReferenceImages }
        );
    }

    private intervalId: any;

    public componentDidMount() {
        this.props.fetchTestRunDetails(this.props.match.params.id);

        this.intervalId = setInterval(() => this.props.fetchTestRunDetails(this.props.match.params.id), 5000);
    }

    public componentWillUnmount() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
        }
    }

    private getTestImages() {

        const images = {};
        this.props.testImages.forEach(ti => {

            if (ti.state !== 'Success' || this.state.showSuccess) {
                images[ti.path] = { 
                    path: ti.path, 
                    name: ti.name, 
                    testRunId: this.props.match.params.id, 
                    testImageUrl: `${IMAGE_BASE_URI}/testimage/${this.props.match.params.id}/${ti.path}`,
                    differenceImageUrl: ti.state === 'Different' ? `${IMAGE_BASE_URI}/diffimage/${this.props.match.params.id}/${ti.path}` : undefined,
                    benchmarkImageUrl: ti.state === 'Different' ? `${IMAGE_BASE_URI}/benchmark/${this.props.application}/${ti.path}` : undefined,
                    state: ti.state,
                    error: ti.error,
                };
            }
        });

        if (this.state.showReferenceImages) {
            this.props.referenceImages.forEach(ri => {
                let img = images[ri.path];
                if (!img) {
                    img = { path: ri.path, testRunId: this.props.match.params.id };
                    images[ri.path] = img;
                }
    
                img.name = img.name || ri.name;
                img.referenceImageUrl = `${IMAGE_BASE_URI}/refimage/${this.props.match.params.id}/${ri.path}`;
            });
        }

        const result = _(Object.keys(images))
            .map(k => images[k])
            .sortBy('path')
            .map(this.getImage)
            .value();

        return result;
    }

    private getImage(i: any): any {
        return <TestImage 
            key={i.path} 
            testRunId={i.testRunId} 
            path={i.path}
            name={i.name} 
            referenceImageUrl={i.referenceImageUrl} 
            testImageUrl={i.testImageUrl} 
            differenceImageUrl={i.differenceImageUrl}
            benchmarkImageUrl={i.benchmarkImageUrl}
            errorMessage={i.error}
            state={i.state}/>;
    }
}

function mapStateToProps(state: any) {
    return{
        isVisible: state.testRunDetails.isVisible,
        testRunId: state.testRunDetails.testRunId,
        name: state.testRunDetails.name,
        testImages: state.testRunDetails.testImages,
        referenceImages: state.testRunDetails.referenceImages,
        application: state.testRunDetails.application
    };
}

function mapDispatchToProps(dispatch: any) {
    return {
        fetchTestRunDetails: (testRunId: number) => dispatch(fetchTestRunDetails(testRunId)),
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(TestRunDetails);