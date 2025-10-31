import React from 'react';
import Header from  './components/Header';
import MapView from './components/MapView';

function App() {
  return (
    <div className="app">
      <Header />
      <div className="main-content">
        <MapView />
      </div>
    </div>
  );
}

export default App; 