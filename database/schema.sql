-- المصدر المعتمد لإنشاء الجداول هو ترحيلات EF Core داخل src/Portfolio.Server/Data/Migrations.
-- هذا الملف محفوظ من نموذج Node.js السابق للرجوع إليه فقط، ولا يشغّل بجانب الترحيلات.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE profiles (
  id SMALLINT PRIMARY KEY CHECK (id = 1),
  full_name_ar TEXT NOT NULL,
  full_name_en TEXT NOT NULL,
  headline_ar TEXT NOT NULL,
  bio_ar TEXT,
  location_ar TEXT NOT NULL,
  email TEXT NOT NULL UNIQUE,
  linkedin_url TEXT,
  photo_url TEXT,
  expected_graduation_year SMALLINT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE projects (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug TEXT NOT NULL UNIQUE,
  title_ar TEXT NOT NULL,
  category_ar TEXT NOT NULL,
  summary_ar TEXT NOT NULL,
  technologies JSONB NOT NULL DEFAULT '[]'::jsonb,
  external_url TEXT,
  cover_image_url TEXT,
  is_published BOOLEAN NOT NULL DEFAULT TRUE,
  sort_order SMALLINT NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT projects_slug_format CHECK (slug ~ '^[a-z0-9-]+$'),
  CONSTRAINT projects_technologies_array CHECK (jsonb_typeof(technologies) = 'array')
);

CREATE TABLE certificates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug TEXT NOT NULL UNIQUE,
  title TEXT NOT NULL,
  issuer TEXT NOT NULL,
  description_ar TEXT NOT NULL,
  issued_on DATE,
  credential_url TEXT,
  is_published BOOLEAN NOT NULL DEFAULT TRUE,
  sort_order SMALLINT NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT certificates_slug_format CHECK (slug ~ '^[a-z0-9-]+$')
);

CREATE TABLE contact_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_name TEXT NOT NULL,
  sender_email TEXT NOT NULL,
  message TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'new',
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT contact_messages_sender_name_length CHECK (char_length(sender_name) BETWEEN 2 AND 120),
  CONSTRAINT contact_messages_sender_email_length CHECK (char_length(sender_email) <= 254),
  CONSTRAINT contact_messages_message_length CHECK (char_length(message) BETWEEN 10 AND 4000),
  CONSTRAINT contact_messages_status CHECK (status IN ('new', 'read', 'archived'))
);

CREATE TABLE admin_users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  google_sub TEXT NOT NULL UNIQUE,
  email TEXT NOT NULL UNIQUE,
  display_name TEXT NOT NULL,
  role TEXT NOT NULL DEFAULT 'owner',
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT admin_users_role CHECK (role IN ('owner', 'editor'))
);

CREATE TABLE admin_sessions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  admin_user_id UUID NOT NULL REFERENCES admin_users(id) ON DELETE CASCADE,
  token_hash CHAR(64) NOT NULL UNIQUE,
  csrf_hash CHAR(64) NOT NULL,
  expires_at TIMESTAMPTZ NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX projects_published_sort_index ON projects (is_published, sort_order);
CREATE INDEX certificates_published_sort_index ON certificates (is_published, sort_order);
CREATE INDEX contact_messages_status_created_index ON contact_messages (status, created_at DESC);
CREATE INDEX admin_sessions_expires_at_index ON admin_sessions (expires_at);
