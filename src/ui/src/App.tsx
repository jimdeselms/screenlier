import * as React from 'react';
import { BrowserRouter as Router, Link, Route } from 'react-router-dom';
import Summaries from './components/Summaries';
import TestRunDetails from './components/TestRunDetails';

import './App.css';

const App = () => (
  <Router>
    <div>
      <Link to='/'>Home</Link>

      <Route path="/" exact={true} component={Summaries} />
      <Route path="/app/:appname" exact={true} component={Summaries} />
      <Route path="/test/:id" component={TestRunDetails} />
    </div>
  </Router>
);

export default App;