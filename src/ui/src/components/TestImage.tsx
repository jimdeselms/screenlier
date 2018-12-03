import * as React from 'react';
import { connect } from 'react-redux';
import { promoteImageToBenchmark } from '../actions/actions';

import './TestImage.css';

export interface TestImageProps {
    testRunId: string,
    name: string,
    path: string,
    state?: string,
    testImageUrl?: string,
    referenceImageUrl?: string,
    differenceImageUrl?: string,
    benchmarkImageUrl?: string,
    errorMessage?: string,
    promoteImageToBenchmark: any
}

class TestImage extends React.Component<TestImageProps> {
    constructor(props: TestImageProps) {
        super(props);
    }
    
    public render() {

        const handleClick = () => {
            this.props.promoteImageToBenchmark(this.props.testRunId, this.props.path);
        };

        const separator = this.props.referenceImageUrl ? <hr /> : undefined;

        const testImage = this.props.state !== 'Error' ? this.thumbnail(this.props.testImageUrl, "Test image") : undefined;
        const referenceImage = this.thumbnail(this.props.referenceImageUrl, 'Reference image');
        const differenceImage = this.thumbnail(this.props.differenceImageUrl, 'Difference image');
        const benchmarkImage = this.thumbnail(this.props.benchmarkImageUrl, 'Benchmark image');
        const error = this.props.errorMessage ? <span className='test-image-error-message'>{this.props.errorMessage}</span> : undefined;

        const promoteBenchmark = this.props.state === 'NoBenchmark' || this.props.state === 'Different' ? <button key='promote' onClick={ handleClick }>Promote image to benchmark</button> : undefined;
        
        const state = this.props.state ? <span key='state' className='test-image-state'>{this.props.state}</span> : undefined;

        return (
            <div>
                <p className='test-image-header'>
                    { separator }
                    {this.props.path} {[ state, promoteBenchmark ]}
                </p>
                <p className='test-image-thumbnails'>
                    { [ testImage, referenceImage, differenceImage, benchmarkImage, error ]}
                </p>
            </div>
        );
    }

    public componentDidMount() {
        //
    }

    private thumbnail(link: string | undefined, altText: string) {
        if (!link) { return undefined; }

        return <a key='testimage' href={link}>
            <img className='test-image-thumbnail' src={link} alt={altText} />
        </a>        
    }
}

function mapStateToProps(state: any) {
    return{
        //
    };
}

function mapDispatchToProps(dispatch: any) {
    return {
        promoteImageToBenchmark: (testRunId: number, path: string) => dispatch(promoteImageToBenchmark(testRunId, path))
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(TestImage);