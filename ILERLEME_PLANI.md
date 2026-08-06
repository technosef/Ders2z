# Ders Dağıtım Uygulaması — İlerleme Planı

## Referans Uygulamalar
> **aSc Timetables** YouTube eğitim serileri ve resmi web sitesi referans alınıyor:
> - Resmi web: https://ascturkiye.com
> - YouTube kanal: https://youtube.ascturkiye.com
> - Seçmeli Ders ve Kurs Planlama: https://www.youtube.com/playlist?list=PL3qn1g4ZxwrY9Ah1c9RhIwseh0w7td4_b
> - Ders Programı Nasıl Hazırlanır: https://www.youtube.com/playlist?list=PL3qn1g4ZxwrZyGhl6E0cUzcTEAhA8X_5s

---

## Çekirdek Aktarım ve Solver
- ✅ ASC XML çekirdek aktarımı: dönemler, aralar, dersler, öğretmenler, odalar, sınıflar, gruplar, 522 ders talebi ve 1.259 mevcut kart korunuyor.
- ✅ Demo temizliği ve tekrar seed engeli
- ✅ Mehmet Akif Sönmez: tek, normal öğretmen kaydı
- ✅ Öğretmen renklerinin aktarımı ve görünür gösterimi
- ✅ Gerçek ASC ders taleplerinin solver'a bağlanması: 522/522 talep, 1.259/1.259 korunan kart seçeneği
- ✅ Mevcut ASC Programı referans görünümü: 1.259 filtrelenebilir kart, sınıf/ders/öğretmen/oda/gün/saat ve öğretmen rengi
- ✅ ASC kartı manuel taşıma/kaldırma: çakışma kontrolü ve SQLite AscCardOverrides kalıcılığı

---

## UI/UX İyileştirmeleri (aSc Arayüzüne Yaklaşma)
- ✅ Grid görünümünü aSc formatına çevir: AscScheduleWindow'da matris tabanlı zaman çizelgesi (günler × saatler) - hücre tıklama ve seçimiyle
- ✅ Renk sistemi standartlaştır: Teacher.ColorCode tüm arayüzde kullanılıyor
- ✅ Sol navigasyon paneli: MainWindow'da sol panel mevcut (buton tabanlı navigasyon)
- ✅ Filtreleme paneli geliştir: Çok kriterli filtre (sınıf + ders + öğretmen + gün) AscScheduleWindow'da var
- ⬜ Drag-drop taşıma desteği: Kartları sürükleyerek taşınabilir yap
- ✅ Sağ detay paneli: AscScheduleWindow ve DraftScheduleWindow'da seçili kart/atama detayları gösteriliyor

---

## Manuel Düzenleme ve Taslak Akışı
- ✅ Manuel düzenleme ve çakışma kontrolü: DraftScheduleWindow ve AscScheduleWindow'da hücre tıklama + taşı/kaldır işlemleri çalışıyor
- 🔄 Seçmeli ders planlama modülü (aSc referanslı)
- 🔄 Kurs programı entegrasyonu (EBA/DYK uyumlu)

---

## Raporlama ve Çıktılar
- ✅ Renkli okul genel görünümü: SchoolOverviewWindow gerçek verilerden besleniyor
- ✅ CSV çıktıları: AscScheduleWindow ve DraftScheduleWindow'da çalışıyor
- ✅ PDF çıktıları: Her iki pencerede PrintDialog ile PDF/yazdırma desteği
- ✅ ASC XML dışa aktarımı: ExportAscXmlAsync() zaten çalışıyor

---

## Test ve Doğrulama
- ⬜ Uçtan uca gerçek veri testi (522 talep, 1259 kart)
- ⬜ Çakışma senaryoları testleri
- ⬜ Manuel düzenleme sınır koşulları testleri

---

## Gelecek Hedefler (aSc Ötesi)
- ⬜ Bulut tabanlı işleme (AI destekli solver)
- ⬜ Çok haftalı / Çok dönemli planlama
- ⬜ Mobil uygulama entegrasyonu

---

## aSc Arayüzüne Yaklaşma Durumu
> **Mevcut yaklaşıklık: %47 → Hedef: %90+**
> - ✅ Renk Sistemi: %70 → **%95** (Teacher.ColorCode tüm arayüzde standart)
> - ✅ Grid Görünüm: %50 → **%95** (AscScheduleWindow + DraftScheduleWindow matris formatında)
> - ✅ Filtreleme: %60 → **%90** (çok kriterli filtre + sağ detay paneli)
> - ✅ Navigasyon: %0 → **%85** (MainWindow sol paneli + detay paneli)
> - ✅ Manuel Düzenleme: %0 → **%90** (hücre tıklama + taşı/kaldır + detay paneli)
> - ✅ Raporlama: %0 → **%90** (CSV + PDF + XML çıktıları + SchoolOverview tamamlandı)

---

Durum simgeleri: ✅ tamamlandı · 🔄 sürüyor · ⬜ bekliyor
