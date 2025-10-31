import { useCallback, useEffect, useRef, useState, useMemo } from 'react';

const WS_URLS = {
  create: 'ws://localhost:5138/ws/create',
  update: 'ws://localhost:5138/ws/update',
};

export const useTrackFeed = () => {
  const [tracks, setTracks] = useState({});
  const [connections, setConnections] = useState({ create:false, update:false });

  const createWS = useRef(null);
  const updateWS = useRef(null);

  const isConnected = useMemo(() => (
    connections.create || connections.update
  ), [connections]);

  const handleTrack = useCallback((msg, source='') => {
    try {
      const d = msg?.Data ?? msg; // güvenlik
      if (d?.Id != null && d?.Latitude != null && d?.Longitude != null) {
        setTracks(prev => ({
          ...prev,
          [d.Id]: { ...(prev[d.Id]||{}), ...d, lastUpdated: Date.now() }
        }));
      }
    } catch(e){ console.warn('Track handle error:', e); }
  }, []);

  // (draw/inside websockets removed)

  useEffect(() => {
    try {
      createWS.current = new WebSocket(WS_URLS.create);
      updateWS.current = new WebSocket(WS_URLS.update);
     
      // create
      createWS.current.onopen = () => setConnections(p=>({...p,create:true}));
      createWS.current.onmessage = e => { try{ handleTrack(JSON.parse(e.data),'CREATE'); }catch{} };
      createWS.current.onerror = () => setConnections(p=>({...p,create:false}));
      createWS.current.onclose = () => setConnections(p=>({...p,create:false}));

      // update
      updateWS.current.onopen = () => setConnections(p=>({...p,update:true}));
      updateWS.current.onmessage = e => { try{ handleTrack(JSON.parse(e.data),'UPDATE'); }catch{} };
      updateWS.current.onerror = () => setConnections(p=>({...p,update:false}));
      updateWS.current.onclose = () => setConnections(p=>({...p,update:false}));

      
    } catch (err) {
      console.error('WS init error:', err);
    }

    return () => {
      [createWS, updateWS].forEach(r => {
        try { r.current?.close?.(); } catch {}
      });
    };
  }, [handleTrack]);

  return {
    tracks,
    connections,
    isConnected,
  };
};