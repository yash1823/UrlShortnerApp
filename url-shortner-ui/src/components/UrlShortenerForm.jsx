import { useState } from 'react';
import axios from 'axios';
import { 
  Button, 
  Form, 
  Alert, 
  Card, 
  Spinner,
  Toast,
  ToastContainer,
  FloatingLabel
} from 'react-bootstrap';
import { 
  Link45deg, 
  Clipboard, 
  CheckCircleFill, 
  ExclamationTriangleFill,
  ArrowRight
} from 'react-bootstrap-icons';
import '../styles/UrlShortener.css';

export default function UrlShortenerForm() {
  const [form, setForm] = useState({
    url: '',
    customCode: '',
    useCustom: false
  });
  const [result, setResult] = useState({
    shortUrl: '',
    loading: false,
    error: null,
    success: false
  });

  const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:44386';

  const handleSubmit = async (e) => {
    e.preventDefault();
    setResult({ ...result, loading: true, error: null });

    try {
      if (!form.url) throw new Error('URL is required');
      if (form.useCustom && !form.customCode) throw new Error('Custom code is required');

      const endpoint = form.useCustom ? '/api/url/custom' : '/api/url/shorten';
      const payload = form.useCustom 
        ? { originalUrl: form.url, customCode: form.customCode }
        : form.url;

      const response = await axios.post(API_BASE + endpoint, payload);
      setResult({
        shortUrl: response.data.shortUrl,
        loading: false,
        error: null,
        success: true
      });
    } catch (err) {
      setResult({
        ...result,
        loading: false,
        error: err.response?.data?.Message || err.message,
        success: false
      });
    }
  };

  const copyToClipboard = () => {
    navigator.clipboard.writeText(result.shortUrl);
    setResult({ ...result, success: true });
  };

  return (
    <div className="dark-theme">
      <div className="url-shortener-container">
        <div className="url-shortener-wrapper">
          <Card className="url-card">
            <Card.Body className="p-4">
              <div className="text-center mb-4">
                <div className="icon-wrapper">
                  <Link45deg size={40} className="text-primary" />
                </div>
                <h2 className="text-white mb-1">URL Shortener</h2>
                <p className="text-muted">Create short links instantly</p>
              </div>

              {result.error && (
                <Alert variant="danger" className="d-flex align-items-center fade-in">
                  <ExclamationTriangleFill className="me-2" />
                  <div>
                    <strong>Error:</strong> {result.error}
                  </div>
                </Alert>
              )}

              <Form onSubmit={handleSubmit}>
                <FloatingLabel controlId="urlInput" label="Enter your long URL" className="mb-3">
                  <Form.Control
                    type="url"
                    placeholder="https://example.com"
                    value={form.url}
                    onChange={(e) => setForm({...form, url: e.target.value})}
                    className="dark-input"
                    required
                  />
                </FloatingLabel>

                <Form.Group className="mb-4">
                  <Form.Check
                    type="switch"
                    id="customSwitch"
                    label="Use custom code"
                    checked={form.useCustom}
                    onChange={(e) => setForm({...form, useCustom: e.target.checked})}
                    className="custom-switch"
                  />
                  {form.useCustom && (
                    <FloatingLabel controlId="customCodeInput" label="Custom code (3-20 chars)" className="mt-2">
                      <Form.Control
                        type="text"
                        placeholder="my-custom-code"
                        value={form.customCode}
                        onChange={(e) => setForm({...form, customCode: e.target.value})}
                        className="dark-input"
                        pattern="[a-zA-Z0-9]{3,10}"
                        title="3-20 alphanumeric characters"
                      />
                    </FloatingLabel>
                  )}
                </Form.Group>

                <Button 
                  variant="primary" 
                  type="submit" 
                  disabled={result.loading}
                  className="w-100 gradient-btn py-3"
                >
                  {result.loading ? (
                    <Spinner as="span" animation="border" size="sm" className="me-2" />
                  ) : (
                    <>
                      Shorten URL <ArrowRight className="ms-2" />
                    </>
                  )}
                </Button>
              </Form>

              {result.shortUrl && (
                <div className="result-container mt-4 p-3">
                  <h5 className="text-white mb-3">Your Short URL:</h5>
                  <div className="d-flex align-items-center">
                    <a 
                      href={result.shortUrl} 
                      target="_blank" 
                      rel="noopener noreferrer"
                      className="short-url text-break me-3"
                    >
                      {result.shortUrl}
                    </a>
                    <Button 
                      variant="outline-light" 
                      size="sm"
                      onClick={copyToClipboard}
                      className="copy-btn"
                    >
                      <Clipboard />
                    </Button>
                  </div>
                </div>
              )}
            </Card.Body>
          </Card>
        </div>
      </div>

      <ToastContainer position="bottom-end" className="p-3">
        <Toast
          show={result.success}
          onClose={() => setResult({...result, success: false})}
          delay={3000}
          autohide
          className="success-toast"
        >
          <Toast.Header closeButton={false} className="bg-success text-white">
            <CheckCircleFill className="me-2" />
            <strong>Success!</strong>
          </Toast.Header>
          <Toast.Body className="text-white">
            {result.shortUrl ? 'URL copied to clipboard!' : 'URL shortened successfully!'}
          </Toast.Body>
        </Toast>
      </ToastContainer>
    </div>
  );
}