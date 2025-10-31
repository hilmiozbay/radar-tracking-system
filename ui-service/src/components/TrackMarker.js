import React, { useEffect, useState, useRef } from 'react';
import { Marker, Popup } from 'react-leaflet';
import L from 'leaflet';

const getTrackIcon = (iffType, highlighted) => {
  let iconUrl = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png';
  if (iffType === 'Ally') iconUrl = 'https://upload.wikimedia.org/wikipedia/commons/2/2c/Military_Symbol_-_Friendly_Unit_%28Solid_Light_1.5x1_Frame%29-_Unspecified_or_Composite_All-Arms_%28NATO_APP-6%29.svg';
  else if (iffType === 'Unknown') iconUrl = 'https://upload.wikimedia.org/wikipedia/commons/2/2c/Military_Symbol_-_Unknown_Aligned_Unit_%28Solid_Quatrefoil_Frame%29-_Unspecified_or_Composite_All-Arms_%28NATO_APP-6A%29.svg';
  else if (iffType === 'Enemy') iconUrl = 'https://upload.wikimedia.org/wikipedia/commons/2/2e/Military_Symbol_-_Hostile_Unit_%28Solid_Diamond_Frame%29-_Unspecified_or_Composite_All-Arms_%28NATO_APP-6A%29.svg';
  else if (iffType === 'Neutral') iconUrl = 'https://upload.wikimedia.org/wikipedia/commons/d/d8/Military_Symbol_-_Neutral_Unit_%28Solid_1.1x1.1_Frame%29-_Unspecified_or_Composite_All-Arms_%28NATO_APP-6A%29.svg';

  const size = highlighted ? [28, 28] : [20, 20];
  return new L.Icon({
    iconUrl, 
    iconRetinaUrl: iconUrl,
    iconSize: size, 
    iconAnchor: [12, 41], 
    popupAnchor: [1, -34], 
    shadowSize: [41, 41],
    className: highlighted ? 'track-marker in-area' : 'track-marker',
  });
};

const TrackMarker = ({ track, isInArea }) => {
  const markerRef = useRef(null);
  const [position, setPosition] = useState([track.Latitude, track.Longitude]);
  const trackIcon = getTrackIcon(track.IffType, isInArea);

  useEffect(() => {
    const newPos = [track.Latitude, track.Longitude];
    setPosition(newPos);
    markerRef.current?.setLatLng(newPos);
  }, [track.Latitude, track.Longitude, track.Id]);

  return (
    <Marker ref={markerRef} position={position} icon={trackIcon}>
      <Popup>
        <div className="popup-content">
          <h3>Track Id: {track.Id}</h3>
          <p>Latitude: {track.Latitude?.toFixed(4)}</p>
          <p>Longitude: {track.Longitude?.toFixed(4)}</p>
          <p>Altitude: {track.Altitude?.toFixed(4)}</p>
          <p>Speed: {track.Speed?.toFixed?.(3) ?? 'N/A'} km/h</p>
          <p>Iff type: {track.IffType || 'Not defined'}</p>
          <p>Callsign: {track.Callsign}</p>
          {isInArea && <p><strong>Inside area</strong></p>}
        </div>
      </Popup>
    </Marker>
  );
};

export default TrackMarker;
