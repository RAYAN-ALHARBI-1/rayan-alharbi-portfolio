INSERT INTO profiles (
  id,
  full_name_ar,
  full_name_en,
  headline_ar,
  bio_ar,
  location_ar,
  email,
  linkedin_url,
  photo_url,
  expected_graduation_year
) VALUES (
  1,
  'ريان صالح الحربي',
  'Rayan Saleh Alharbi',
  'طالب نظم معلومات حاسوبية ومطور برمجيات',
  'أطوّر تطبيقات ويب متكاملة وأعمل على قواعد البيانات والخدمات الخلفية، مع اهتمام بتحويل المتطلبات إلى تجربة عملية واضحة قابلة للتوسع.',
  'المدينة المنورة، المملكة العربية السعودية',
  'Rayanalharbi272@gmail.com',
  'https://www.linkedin.com/in/rayan-alharbi-617b1a3a6',
  '/rayan-alharbi-portrait.jpeg',
  2027
) ON CONFLICT (id) DO UPDATE SET
  full_name_ar = EXCLUDED.full_name_ar,
  full_name_en = EXCLUDED.full_name_en,
  headline_ar = EXCLUDED.headline_ar,
  bio_ar = EXCLUDED.bio_ar,
  location_ar = EXCLUDED.location_ar,
  email = EXCLUDED.email,
  linkedin_url = EXCLUDED.linkedin_url,
  photo_url = EXCLUDED.photo_url,
  expected_graduation_year = EXCLUDED.expected_graduation_year,
  updated_at = NOW();

INSERT INTO projects (
  slug,
  title_ar,
  category_ar,
  summary_ar,
  technologies,
  external_url,
  sort_order
) VALUES
  (
    'student-performance-analytics',
    'تحليلات الأداء الأكاديمي للطلاب',
    'تحليل بيانات وتنقيب بيانات',
    'مشروع مستودع بيانات وتنقيب بيانات حلّل محددات الأداء الأكاديمي اعتمادًا على 6,607 سجلات و20 خاصية، مع معالجة البيانات الناقصة وعدم توازن الفئات.',
    '["Python", "Scikit-learn", "Pandas", "Data Visualization"]'::jsonb,
    'https://drive.google.com/drive/folders/1F4AhHa_Bn77G_ZPqcFpe4QJtS0hIYTox?usp=drive_link',
    10
  ),
  (
    'car-sales-booking-platform',
    'منصة مبيعات وحجز السيارات',
    'تطبيق ويب وجوال متكامل',
    'تطبيق ويب وجوال متكامل لإدارة مبيعات السيارات والحجوزات، مع خلفية .NET Core وقاعدة SQL Server وتطبيق .NET MAUI متصل بالبيانات والخدمات نفسها.',
    '[".NET Core", "SQL Server", ".NET MAUI", "APIs"]'::jsonb,
    'https://drive.google.com/drive/folders/1WDSQWvHcaYM8iul6sgvw9m6q7Xuvx9Wm?usp=drive_link',
    20
  )
ON CONFLICT (slug) DO UPDATE SET
  title_ar = EXCLUDED.title_ar,
  category_ar = EXCLUDED.category_ar,
  summary_ar = EXCLUDED.summary_ar,
  technologies = EXCLUDED.technologies,
  external_url = EXCLUDED.external_url,
  sort_order = EXCLUDED.sort_order,
  updated_at = NOW();

INSERT INTO certificates (
  slug,
  title,
  issuer,
  description_ar,
  issued_on,
  credential_url,
  sort_order
) VALUES
  ('ibm-lifelong-professional-skills', 'Lifelong Professional Skills', 'IBM SkillsBuild', 'إتمام برنامج المهارات المهنية المستمرة، الذي يركز على تطوير مهارات العمل والتواصل والتعلم المهني.', '2026-07-19', NULL, 10),
  ('robotics-bootcamp', 'Robotics Bootcamp Certificates', 'Robotics Bootcamp', 'مجموعة شهادات متخصصة في أساسيات الروبوتات والأنظمة الذكية وتطبيقاتها العملية.', NULL, '/certificates/Robotics%20Bootcamp%20Certificates%20-%20RAYAN%20SALEH%20ALHARBI.pdf', 20),
  ('introduction-to-robotics', 'Introduction to Robotics', 'Robotics', 'مدخل إلى مفاهيم الروبوتات ومكوّناتها وطريقة عمل الأنظمة الروبوتية.', NULL, '/certificates/Introduction%20to%20robotics%20-%20Rayan%20Saleh%20Alharbi.pdf', 30),
  ('generative-ai-agents-robotics', 'From Generative AI to Agents & Robotics', 'AI & Robotics', 'استكشاف انتقال الذكاء الاصطناعي التوليدي إلى الوكلاء والأنظمة الروبوتية.', NULL, '/certificates/From%20Generative%20AI%20to%20Agents%20%26%20Robotics%20-%20Rayan%20Saleh%20Alharbi.pdf', 40),
  ('robotics-security', 'Robotics Security', 'Robotics Security', 'مبادئ حماية الأنظمة الروبوتية والاعتبارات الأمنية في بيئاتها المتصلة.', NULL, '/certificates/Robotics%20security%20-%20Rayan%20Saleh%20Alharbi.pdf', 50),
  ('uav-swarm-deployment', 'UAV Swarm Deployment', 'UAV Systems', 'تطبيقات نشر أسراب الطائرات المسيّرة والتنسيق بين الأنظمة ذاتية التشغيل.', NULL, '/certificates/UAV%20Swarm%20Deployment%20-%20Rayan%20Saleh%20Alharbi.pdf', 60),
  ('bio-inspired-swarms', 'From One Robot to Bio-Inspired Swarms', 'Bio-Inspired Systems', 'مفاهيم الانتقال من الروبوتات الفردية إلى أسراب مستوحاة من الأنظمة الحيوية.', NULL, '/certificates/from%20one%20robot%20to%20bio%20inspired%20swarms%20-%20Rayan%20Saleh%20Alharbi.pdf', 70)
ON CONFLICT (slug) DO UPDATE SET
  title = EXCLUDED.title,
  issuer = EXCLUDED.issuer,
  description_ar = EXCLUDED.description_ar,
  issued_on = EXCLUDED.issued_on,
  credential_url = EXCLUDED.credential_url,
  sort_order = EXCLUDED.sort_order,
  updated_at = NOW();
