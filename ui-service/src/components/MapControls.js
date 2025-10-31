import React from 'react';

const MapControls = ({ 
  isConnected, 
  trackCount
}) => {
  return (
    <div className="map-controls">
      <div className="connection-status">
        <span className={`status-indicator ${isConnected ? 'connected' : 'disconnected'}`}>
          {isConnected ? 'WebSocket Connected' : 'WebSocket in not connected'}
        </span>
      </div>
      
      <div className="stats">
        <span className="stat">Tracks: {trackCount}</span>
      </div>
    </div>
  );
};

export default MapControls; 