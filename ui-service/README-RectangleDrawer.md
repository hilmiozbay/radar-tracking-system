# Simple Rectangle Drawer - React Leaflet

Bu uygulama, React Leaflet kullanarak kullanıcıların harita üzerinde basit dikdörtgenler çizmesine olanak tanır.

## 🎯 Özellikler

- ✅ **İki tıklama ile dikdörtgen çizimi**
- ✅ **Gerçek zamanlı preview** (mouse hareket ederken)
- ✅ **Çoklu dikdörtgen desteği**
- ✅ **Dikdörtgen silme/temizleme**
- ✅ **Koordinat bilgileri** (lat/lng boundaries)
- ✅ **Responsive tasarım**

## 🚀 Kullanım

### Test uygulamasını çalıştırmak için:

```bash
# UI service dizinine git
cd ui-service

# Geliştirme sunucusunu başlat
npm start
```

### Kendi uygulamanızda kullanmak için:

```jsx
import SimpleRectangleDrawer from './components/SimpleRectangleDrawer';

function App() {
  return (
    <div>
      <SimpleRectangleDrawer />
    </div>
  );
}
```

## 📝 Nasıl Çalışır

### 1. Dikdörtgen Çizimi
1. **"Dikdörtgen Çiz"** butonuna tıklayın
2. Harita üzerinde **ilk noktaya** tıklayın (başlangıç köşesi)
3. Mouse'u hareket ettirin (preview göreceksiniz)
4. **İkinci noktaya** tıklayın (bitiş köşesi)
5. Dikdörtgen otomatik olarak tamamlanır

### 2. Dikdörtgen Yönetimi
- **Tek silme**: Her dikdörtgenin yanındaki ❌ butonuna tıklayın
- **Toplu silme**: "Tümünü Temizle" butonunu kullanın
- **İptal**: Çizim modundayken "Çizimi İptal Et" butonuna tıklayın

## 🎨 Görsel Özellikler

- **Mavi dikdörtgenler** (yarı şeffaf dolgu)
- **Kontrol paneli** (sağ üst köşe)
- **Ankara merkezli** harita (zoom seviyesi 6)
- **OpenStreetMap** tile layer

## 📊 Koordinat Bilgileri

Her dikdörtgen için şu bilgiler döndürülür:

```javascript
{
  minLatitude: 39.123,
  maxLatitude: 40.456,
  minLongitude: 32.789,
  maxLongitude: 33.012
}
```

## 🔧 Özelleştirme

### Harita Merkezi Değiştirme:
```jsx
<MapContainer
  center={[latitude, longitude]} // İstediğiniz koordinat
  zoom={zoomLevel}               // Zoom seviyesi (1-18)
>
```

### Dikdörtgen Rengi Değiştirme:
```jsx
pathOptions={{
  color: '#ff0000',        // Kenarlık rengi
  fillColor: '#ff0000',    // Dolgu rengi
  fillOpacity: 0.3,        // Şeffaflık (0-1)
  weight: 3                // Kenarlık kalınlığı
}}
```

### Callback Fonksiyonu:
```jsx
const handleRectangleComplete = (rectangleData) => {
  console.log('Yeni dikdörtgen:', rectangleData);
  // Burada API'ye gönderebilir, state'e kaydedebilirsiniz
};

<SimpleRectangleDrawer onRectangleComplete={handleRectangleComplete} />
```

## 🔗 Kafka Entegrasyonu İçin

Geometry servisinizle entegre etmek için:

```javascript
const sendToKafka = async (rectangleData) => {
  const shapeMessage = {
    name: `Area ${Date.now()}`,
    ...rectangleData,
    action: "create"
  };
  
  // Kafka producer'a gönder
  await fetch('/api/kafka/draw_topic', {
    method: 'POST',
    body: JSON.stringify(shapeMessage)
  });
};
```

## 📁 Dosya Yapısı

```
ui-service/
├── src/
│   ├── components/
│   │   └── SimpleRectangleDrawer.js  # Ana component
│   └── TestRectangleApp.js           # Test uygulaması
└── README-RectangleDrawer.md         # Bu dosya
```

## 🎯 Gelecek Özellikler

- [ ] Polygon çizimi
- [ ] Çember çizimi  
- [ ] Dikdörtgen düzenleme
- [ ] Export/Import fonksiyonları
- [ ] Ölçüm bilgileri (alan, mesafe)

---

✨ **Basit, hızlı ve etkili dikdörtgen çizim aracı!** ✨
