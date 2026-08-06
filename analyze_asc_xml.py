import xml.etree.ElementTree as ET

tree = ET.parse(r'C:/ikizsoft/Ders2z/asc-map-roundtrip.xml')
root = tree.getroot()

print('=== aSc Timetables XML Analizi ===')
print()

tags = {}
for tag in root:
    tags[tag.tag] = len(list(tag))

sorted_tags = sorted(tags.items(), key=lambda x: -x[1])
for k, v in sorted_tags:
    print(f'  <{k}>: {v} eleman')
print()

print('=== Zaman Dilimleri (Periods) ===')
for p in root.findall('periods/period'):
    print(f'  {p.attrib.get("period","")} - {p.attrib.get("name","")}')
print()

print('=== Tenefusler (Breaks) ===')
for b in root.findall('breaks/break'):
    print(f'  {b.attrib.get("break","")}. saat sonrasi: {b.attrib.get("name","")} ({b.attrib.get("starttime","")}-{b.attrib.get("endtime","")})')
print()

print('=== Ilk 10 Ders (Subjects) ===')
subjects = root.findall('subjects/subject')
for s in subjects[:10]:
    print(f'  {s.attrib.get("short","")} - {s.attrib.get("name","")}')
print(f'  ... ve {len(subjects)-10} ders daha')
print()

print('=== Ilk 10 Ogretmen (Teachers) ===')
teachers = root.findall('teachers/teacher')
for t in teachers[:10]:
    print(f'  {t.attrib.get("short","")} - {t.attrib.get("name","")} (renk: {t.attrib.get("color","")})')
print(f'  ... ve {len(teachers)-10} ogretmen daha')
print()

print('=== Ilk 10 Sinif (Classes) ===')
classes = root.findall('classes/class')
for c in classes[:10]:
    print(f'  {c.attrib.get("short","")} - {c.attrib.get("name","")}')
print(f'  ... ve {len(classes)-10} sinif daha')
print()

print('=== Ilk 10 Derslik/Oda (Classrooms) ===')
classrooms = root.findall('classrooms/classroom')
for c in classrooms[:10]:
    print(f'  {c.attrib.get("short","")} - {c.attrib.get("name","")} (kapasite: {c.attrib.get("capacity","N/A")})')
print(f'  ... ve {len(classrooms)-10} derslik daha')
print()

print('=== Ilk 10 Grup (Groups) ===')
groups = root.findall('groups/group')
for g in groups[:10]:
    print(f'  {g.attrib.get("name","")} (sinif: {g.attrib.get("classid","")})')
print(f'  ... ve {len(groups)-10} grup daha')
print()

print('=== Ilk 10 Ders Talebi (Lessons) ===')
lessons = root.findall('lessons/lesson')
for l in lessons[:10]:
    print(f'  Sinif:{l.attrib.get("classids","")} Ders:{l.attrib.get("subjectid","")} Ogretmen:{l.attrib.get("teacherids","")} Saat:{l.attrib.get("periodsperweek","")} Desen:{l.attrib.get("periodspercard","")}')
print(f'  ... ve {len(lessons)-10} talep daha')
print()

print('=== Ilk 10 Program Karti (Cards) ===')
cards = root.findall('cards/card')
for c in cards[:10]:
    print(f'  Gun:{c.attrib.get("days","")} Saat:{c.attrib.get("period","")} Sinif:{c.attrib.get("classids","")} Ders:{c.attrib.get("subjectid","")} Ogretmen:{c.attrib.get("teacherids","")} Derslik:{c.attrib.get("classroomids","")}')
print(f'  ... ve {len(cards)-10} kart daha')
print()

# Analiz
print('=== DERINLEMESINE ANALIZ ===')
print()

# 1. Ders taleplerinin blok desen analizi
print('1. Blok Desen Dagilimi (Lessons):')
patterns = {}
for l in lessons:
    ppc = l.attrib.get('periodspercard', '')
    patterns[ppc] = patterns.get(ppc, 0) + 1
for p, c in sorted(patterns.items(), key=lambda x: -x[1]):
    print(f'   periodspercard="{p}": {c} talep')
print()

# 2. Gün bazlı kart dağılımı
print('2. Gun Bazli Kart Dagilimi (Cards):')
day_counts = {}
for c in cards:
    days = c.attrib.get('days', '')
    day_counts[days] = day_counts.get(days, 0) + 1
for d, c in sorted(day_counts.items(), key=lambda x: -x[1]):
    print(f'   Gun {d}: {c} kart')
print()

# 3. Öğretmen renk kullanımı
print('3. Ogretmen Renk Kullanimi:')
with_color = sum(1 for t in teachers if t.attrib.get('color'))
without_color = len(teachers) - with_color
print(f'   Renkli: {with_color}, Renksiz: {without_color}')
print()

# 4. Öğretmen özlük bilgileri
print('4. Ogretmen Ozluk Bilgileri Ornekleri:')
for t in teachers[:3]:
    name = t.attrib.get('name', '')
    color = t.attrib.get('color', '')
    partner = t.attrib.get('partner', '')
    email = t.attrib.get('email', '')
    mobile = t.attrib.get('mobile', '')
    max_days = t.attrib.get('maxdays', '')
    max_lessons = t.attrib.get('maxlessonsperweek', '')
    print(f'   {name}: renk={color}, partner={partner}, email={email}, mobile={mobile}, max_gun={max_days}, max_saat={max_lessons}')
print()

# 5. Sınıf detayları
print('5. Sinif Detay Ornekleri:')
for c in classes[:3]:
    print(f'   {c.attrib.get("name","")}: teacherid={c.attrib.get("teacherid","")}, partner={c.attrib.get("partner","")}, classroomid={c.attrib.get("classroomids","")}')
print()

# 6. Derslik detayları
print('6. Derslik Detay Ornekleri:')
for c in classrooms[:5]:
    print(f'   {c.attrib.get("name","")}: kapasite={c.attrib.get("capacity","")}, partner={c.attrib.get("partner","")}')
print()

# 7. Ders taleplerinde öğretmen olmayanlar
no_teacher = sum(1 for l in lessons if not l.attrib.get('teacherids'))
print(f'7. Ogretmen atanmamis ders talebi sayisi: {no_teacher}')
print()

# 8. Kartlarda derslik kullanımı
with_room = sum(1 for c in cards if c.attrib.get('classroomids'))
without_room = len(cards) - with_room
print(f'8. Derslik atamali kart: {with_room}, Dersliksiz kart: {without_room}')
print()

# 9. Öğretmenlerin maxlessonsperweek değerleri
max_lessons_values = {}
for t in teachers:
    ml = t.attrib.get('maxlessonsperweek', '')
    if ml:
        max_lessons_values[ml] = max_lessons_values.get(ml, 0) + 1
print('9. Ogretmen haftalik max ders saat dagilimi:')
for v, c in sorted(max_lessons_values.items(), key=lambda x: int(x[0])):
    print(f'   maxlessonsperweek={v}: {c} ogretmen')
