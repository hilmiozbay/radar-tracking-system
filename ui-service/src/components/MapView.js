import React, { useState, useMemo } from 'react';
import { MapContainer, TileLayer, Rectangle, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import '../styles.css';
import { useTrackFeed } from '../hooks/useTrackFeed';
import MapControls from './MapControls';
import TrackMarker from './TrackMarker';

const MapView = () => {
  const [rectangles, setRectangles] = useState([]);
  const [isDrawing, setIsDrawing] = useState(false);
  const [startPoint, setStartPoint] = useState(null);
  const [alertMessage, setAlertMessage] = useState('');

  const { tracks, isConnected, sendAreaMessage, alertedTracks } = useTrackFeed(); 

  const turkeyCenter = [38.5, 35];

  const tracksArray = useMemo(() => { 
    const array = Object.values(tracks);
    console.log('Map view render - toplam track sayısı:', array.length);
    return array;
  }, [tracks]);

  const getRectangleBounds = ([[lat1, lng1], [lat2, lng2]]) => ({
    minLat: Math.min(lat1, lat2),
    maxLat: Math.max(lat1, lat2),
    minLng: Math.min(lng1, lng2),
    maxLng: Math.max(lng1, lng2),
});


  const MapEvents = () => {
  useMapEvents({
    click: (e) => {
      if (!isDrawing) return;

      const point = [e.latlng.lat, e.latlng.lng];

      if (!startPoint) {
        setStartPoint(point);
      } else {
        const newRectangle = [startPoint, point];
        const updatedRectangles = [...rectangles, newRectangle];
        setRectangles(updatedRectangles);
        setAlertMessage('Rectangle added successfully!');
        setTimeout(() => setAlertMessage(''), 2000);

        // Sıfırla
        setStartPoint(null);
        setIsDrawing(false);

        // Dikdörtgen sınırlarını hesapla ve WebSocket'e gönder
        const bounds = getRectangleBounds(newRectangle);
        console.log('Dikdörtgen çizildi, sınırlar:', bounds);
        sendAreaMessage({
          MinLat: bounds.minLat,
          MaxLat: bounds.maxLat,
          MinLng: bounds.minLng,
          MaxLng: bounds.maxLng
        });
      }
    }
  });
  return null;
};


  return (
    <div className="container">
          {alertMessage && (
      <div className="alert-box">
        {alertMessage}
      </div>
    )}
      {/* Basit kontrol paneli */}
      <div className="control-panel">
        <button 
          onClick={() => setIsDrawing(!isDrawing)}
          className={`btn ${isDrawing ? 'btn-stop' : 'btn-start'}`}
        >
          {isDrawing ? 'Stop' : 'Start Drawing'}
        </button>
        
        <button 
          onClick={() => {
            setRectangles([]);
            setStartPoint(null);
            setIsDrawing(false);
          }}
          className="btn btn-clear"
        >
          Clear
        </button>
        
        <div className="status">
          <p>Rectangle Count: <strong>{rectangles.length}</strong></p>
          {isDrawing && (
            <p>Click on the map</p>
          )}
          {startPoint && (
            <p>Select the second point</p>
          )}
        </div>
      </div>

      {/* Harita */}
      <MapContainer
        center={turkeyCenter} // Ankara koordinatları
        zoom={5.3}
        className="map-container"
      >
        <MapControls
        isConnected={isConnected}
        trackCount={tracksArray.length}
      />

        {tracksArray.map(track => (
          <TrackMarker
            key={track.Id}
            track={track}
            isInArea={alertedTracks.has(track.Id)}
          />
        ))}

        <TileLayer 
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        />
        <MapEvents />
        
        {/* Çizilen dikdörtgenler */}
        {rectangles.map((rect, index) => (
          <Rectangle
            key={index}
            bounds={rect}
            pathOptions={{ 
              color: '#3498db', 
              weight: 2,
              fillColor: '#3498db',
              fillOpacity: 0.1 
            }}
          />
        ))}
      </MapContainer>
    </div>
  );
};

export default MapView;