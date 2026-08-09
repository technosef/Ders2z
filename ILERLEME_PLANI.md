# Ders Dağıtım Uygulaması — İlerleme Planı

> Kalıcı teknik bağlam ve sıradaki kesin işler: `PROJE_HAFIZASI.md`

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
- ✅ ASC kartı manuel taşıma/kaldırma/geri alma: sınıf-öğretmen-kaynak çakışma kontrolü, SQLite AscCardOverrides kalıcılığı ve taslak yeniden üretiminde koruma
- ✅ ASC ilişki kimlikleri doğrulandı/onarıldı: 522/522 talep, 1.259/1.259 kart ve 46 oda gerçek veritabanında eşleşiyor

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
- ✅ Manuel düzenleme ve çakışma kontrolü: DraftScheduleWindow ve AscScheduleWindow'da hücre tıklama + kalıcı taşı/kaldır/geri alma işlemleri çalışıyor
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
- 🔄 Uçtan uca gerçek veri testi (522 talep, 1.259 kart): veri/override döngüsü geçti, tam kullanıcı etkileşimi sürüyor
- ✅ Çakışma senaryoları: sınıf, öğretmen, kaynak kapasitesi, blok sınırı ve kesin kilit denetimleri bağlı
- ✅ Manuel düzenleme sınır koşulları: taşı → yeniden oku → kaldır → yeniden oku → geri al → yeniden oku testi geçti

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

## 9 Ağustos 2026 Doğrulanan İlerleme

- ✅ MainWindow/DataManagement/SchoolOverview içindeki kalan demo ekran dili ve sabit filtreler gerçek kurum ASC verisine çevrildi.
- ✅ SchoolOverview alan/program/sınıf/öğretmen/kaynak filtreleri gerçek kayıtlardan doluyor ve 1.259 ASC kartı üzerinden liste üretiyor.
- ✅ ASC XML dışa aktarımında subject/teacher/class/classroom external ID'leri korunuyor; yeni GUID yazılmıyor.
- ✅ `AscCardOverrides` taşınan/kaldırılan kartları export cards çıktısına uyguluyor.
- ✅ Export round-trip: 145 grup / 522 lesson / 1.259 card doğrulandı.
- ✅ Override kopya DB testi: kaldırma sonrası 1.258 card, taşınan kart export'ta period/days güncel.
- ✅ Derleme: `dotnet build DersDagitim.sln` başarılı, 0 hata, 0 uyarı.

## 9 Ağustos 2026 Kalıcı Öğretmen Devri

- ✅ `AscCardTeacherOverrides` kalıcı veri modeli eklendi.
- ✅ Taslak ekranında seçili ders başka öğretmene devredilebilir hale getirildi.
- ✅ Devretme işleminde öğretmen çakışması, kesin kilit, kaynak kapasitesi ve haftalık yük kontrolleri uygulanıyor.
- ✅ Devredilen öğretmen; taslak, mevcut ASC görünümü, öğretmen raporu, sınıf/kaynak çizelgeleri ve çakışma kontrolünde görünür hale getirildi.
- ✅ ASC XML export `teacherids` değerini devredilen öğretmenin ASC external ID'siyle güncelliyor.
- ✅ Doğrulama: canlı DB 522/1259 solver sayıları korundu; kopya DB öğretmen devri ve XML export testi geçti.
- ✅ Derleme ve EXE smoke testi geçti.

## 9 Ağustos 2026 Git Temizliği

- ✅ `.gitignore` eklendi.
- ✅ `.vs`, `bin`, `obj` gibi IDE/build çıktıları dosya sisteminden silinmeden git takibinden çıkarıldı.
- ✅ GitHub remote bağlandı: `https://github.com/technosef/Ders2z.git`.
- ✅ Remote geçmişi korunarak merge edildi ve `main` branch GitHub'a push edildi.

## 9 Ağustos 2026 SQLite Haftalık Uygunluk Kayıtları

- ✅ SQLite'a bağlı haftalık öğretmen uygunluk/kilit kayıt modeli eklendi.
- ✅ Uygunluk ekranından öğretmen/gün/saat/durum/gerekçe seçilerek kayıt eklenebilir ve seçili kayıt silinebilir.
- ✅ Taslak üretimi artık SQLite uygunluk kayıtlarını solver'a geçiriyor.
- ✅ Doğrulama: kopya DB kayıt ekle/sil testi geçti; canlı DB migration oluştu; derleme ve EXE smoke testi geçti.
- ✅ EXE smoke testi geçti; uygulama kısa süre çalışıp temiz kapatıldı.
- ✅ Yerel git commit: `dad5979` (`Gercek veri ekranlari ve hakkinda paneli`).
- ⚠️ Git push bekliyor; repoda `origin` remote tanımlı değil.
- ✅ EXE smoke: WPF uygulaması başlatıldı ve temiz kapatıldı.

## 9 Ağustos 2026 Kullanıcı Ekran Düzeltmeleri

- ✅ Haftalık uygunluk ızgarası artık boş görünmez; gerçek öğretmenleri ve kısıt durumu bilgisini gösterir.
- ✅ Taslak çizelgede okul geneli eşzamanlı ders sayısı çakışma olarak gösterilmez; gerçek çakışma kontrolünden ayrıldı.
- ✅ Çakışma kontrolü gerçek sınıf/öğretmen/kaynak bindirmelerini analiz eder ve yerleşmeyen taleplerle birlikte listeler.
- ✅ Öğretmen programı ve ders yükü ekranı tek örnek öğretmene bağlı değildir; gerçek ASC/taslak verisinden öğretmen seçilebilir rapor üretir.
- ✅ Derleme ve EXE smoke testi geçti.

## 9 Ağustos 2026 Ek Rapor Düzeltmesi

- ✅ `TeacherReportSample` kaldırıldı; öğretmen raporu gerçek veriden bağımsız örnek kayda dönmez.
- ✅ Son derleme ve EXE smoke testi başarılı.

## 9 Ağustos 2026 Manuel Kullanım ve Okul Geneli Düzeltmeleri

- ✅ Okul genel görünümü duplicate sınıf adı hatasına dayanıklı hale getirildi.
- ✅ Tüm okul için renkli HTML program dosyası export eklendi.
- ✅ Taslak çizelgeye sınıf/öğretmen filtresi eklendi; filtreli tek ders hücrelerinde sürükle-bırak gün/saat taşıma ve seçili atama kaldırma kullanılabilir.
- ✅ Taslak CSV çıktısında GUID yerine sınıf/ders/öğretmen adları yazılıyor.
- ✅ Derleme ve EXE smoke testi geçti.
- ⬜ Kalıcı öğretmen devri: `AscCardTeacherOverrides` benzeri tablo, solver uygulaması ve ASC export `teacherids` güncellemesi eklenecek.

## 9 Ağustos 2026 Sınıf ve Kaynak Program Çıktıları

- ✅ Sınıfların haftalık ders programı izlenebilir hale getirildi.
- ✅ Laboratuvar/kaynak haftalık ders programı izlenebilir hale getirildi.
- ✅ Her iki görünüm için CSV ve PDF/yazdırma desteği eklendi.
- ✅ `Çizelgeler` menüsü sınıf/kaynak program ekranına bağlandı.
- ✅ Derleme ve EXE smoke testi geçti.

## 9 Ağustos 2026 Güncelleme ve Hakkında

- ✅ Ana menüye `Güncelleme / Hakkında` ekranı eklendi.
- ✅ Hakkında bilgileri eklendi: `Hazırlayan: Mehmet Akif SÖNMEZ`, `İkizsoft Bilişim Hizmetleri`, `www.ikizsoft.com`.
- ✅ WPF sürümü `0.9.0-dev` olarak proje metadata'sına bağlandı.
- ✅ Güncelleme kontrol butonu eklendi; otomatik güncelleme altyapısı için ileride bağlanacak kanal bilgisi ekranda gösteriliyor.
- ✅ Derleme: `dotnet build DersDagitim.sln` başarılı, 0 hata, 0 uyarı.

## 9 Ağustos 2026 Manuel Düzenleme Görünürlüğü

- ✅ Taslak ekranının sağ paneline açık `Manuel düzenleme` bölümü eklendi.
- ✅ Seçili ders gün/saat seçilerek butonla taşınabilir hale getirildi.
- ✅ Seçili atamayı kaldırma manuel panelde görünür hale getirildi.
- ✅ Derleme: `dotnet build DersDagitim.sln` başarılı, 0 hata, 0 uyarı.

## 9 Ağustos 2026 Kullanım Kılavuzu

- ✅ Resimli kullanım kılavuzu üretildi: `docs/DersDagitim_Kullanim_Kilavuzu.docx`.
- ✅ İçerik sıfırdan kullanan kişi için adım adım yazıldı.
- ✅ Kılavuzda 14 bölüm, 5 tablo ve 6 görsel var.
- ⚠️ Render/PDF kalite kontrolü ortamda `soffice`/`winword` bulunmadığı için yapılamadı; DOCX yapısal kontrolü geçti.

## 9 Ağustos 2026 Sınıf Alan Ataması

- ✅ Ana Excel dosyasından Bilişim alan bilgisi okundu.
- ✅ ASC sınıf kodları ve ders içeriğiyle tüm sınıfların alanı atandı.
- ✅ Canlı SQLite sonucu: 46/46 sınıf alanlı; boş `Department` kalmadı.
- ✅ Dağılım: Bilişim 20, Elektrik-Elektronik 14, Biyomedikal 12.
- ✅ Tekrarlanabilir atama betiği eklendi: `tools/apply_class_departments_from_excel.py`.
- ✅ Derleme: `dotnet build DersDagitim.sln` başarılı, 0 hata, 0 uyarı.

## 9 Agustos 2026 Sinif Kapi Listeleri ve Sinif Ogretmenligi

- Tamamlandi: Sinif/kaynak cizelgesi ekranina kapilara asilacak sinif bazli haftalik HTML liste cikisi eklendi.
- Tamamlandi: Belirli sinif seciliyken sinif ogretmeni atama ve atamayi kaldirma UI'si eklendi.
- Tamamlandi: `ClassTeacherAssignments` SQLite kalici kayit modeli eklendi.
- Dogrulandi: Canli DB'de tablo olustu; mevcut atama sayisi 0. Sinif ogretmenlikleri kullanici tarafindan henuz atanmamis.
- Dogrulandi: 522 lesson / 1259 card solver sayilari korundu.
- Dogrulandi: `dotnet build DersDagitim.sln` basarili, 0 hata, 0 uyari.
- Devam: Kullanici sinif cizelgesi ekranindan her sinifin ogretmenini tek tek atayacak veya sonraki adimda Excel'den toplu atama araci yazilacak.

## 9 Agustos 2026 aSc Tarzi Sinif/Lab Plan Ciktisi

- Tamamlandi: `2026-LABORATUVAR.pdf` referansindaki A4 yatay tablo yapisi sinif/lab ciktisina uygulandi.
- Tamamlandi: PDF/yazici ciktisi DataGrid yerine cok sayfali haftalik plan dokumani basar.
- Tamamlandi: HTML cikti da ayni sayfa duzeniyle sinif ve kaynak/lab modlarini destekler.
- Tamamlandi: Ogle aralari dikey kolon, ders bloklari yatay birlesik hucre olarak uretilir.
- Dogrulandi: Derleme basarili, 0 hata, 0 uyari.

## 9 Agustos 2026 Tam Ekran Pencere Acilisi

- Tamamlandi: Ana ekran ve alt ekranlar varsayilan olarak tam ekran/maximized acilir hale getirildi.
- Dogrulandi: Derleme basarili, 0 hata, 0 uyari; EXE smoke testi gecti.
