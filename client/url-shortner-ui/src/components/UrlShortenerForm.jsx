import { useState } from 'react';
import axios from 'axios';
import { Button, Form, Alert, Container, Card } from 'react-bootstrap';
import { Link45deg, Clipboard } from 'react-bootstrap-icons';
export default function UrlShortenerForm() {
  const [url, setUrl] = useState('');
  const [shortUrl, setShortUrl] = useState('');
  const [customCode, setCustomCode] = useState('');
  const [error, setError] = useState('');
  const [isCustom, setIsCustom] = useState(false);

  const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5000';

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    
    try {
      let response;
      if (isCustom && customCode) {
        response = await axios.post(`${API_BASE}/url/custom`, {
          originalUrl: url,
          customCode
        });
      } else {
        response = await axios.post(`${API_BASE}/url/shorten`, url);
      }
      
      setShortUrl(response.data.shortUrl);
    } catch (err) {
      setError(err.response?.data || 'Error shortening URL');
    }
  };

  const copyToClipboard = () => {
    navigator.clipboard.writeText(shortUrl);
  };

  return (
    <Container className="mt-5" style={{ maxWidth: '600px' }}>
      <Card className="shadow">
        <Card.Body>
          <h2 className="mb-4 text-center">
            <Link45deg className="me-2" />
            URL Shortener
          </h2>

          {error && <Alert variant="danger">{error}</Alert>}

          <Form onSubmit={handleSubmit}>
            <Form.Group className="mb-3">
              <Form.Label>Long URL</Form.Label>
              <Form.Control
                type="url"
                placeholder="https://example.com/very-long-url"
                value={url}
                onChange={(e) => setUrl(e.target.value)}
                required
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Check
                type="switch"
                label="Custom Short Code"
                checked={isCustom}
                onChange={(e) => setIsCustom(e.target.checked)}
              />
              {isCustom && (
                <Form.Control
                  type="text"
                  placeholder="my-custom-link"
                  value={customCode}
                  onChange={(e) => setCustomCode(e.target.value)}
                  pattern="[A-Za-z0-9]{3,20}"
                  title="3-20 alphanumeric characters"
                />
              )}
            </Form.Group>

            <Button variant="primary" type="submit" className="w-100">
              Shorten URL
            </Button>
          </Form>

          {shortUrl && (
            <div className="mt-4 text-center">
              <h5>Your Short URL:</h5>
              <div className="d-flex align-items-center justify-content-center">
                <a
                  href={shortUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="me-2"
                >
                  {shortUrl}
                </a>
                <Button variant="outline-secondary" size="sm" onClick={copyToClipboard}>
                  <Clipboard />
                </Button>
              </div>
            </div>
          )}
        </Card.Body>
      </Card>
    </Container>
  );
}