import React, { useRef, useState } from 'react';
import { Form, InputGroup, Button } from 'react-bootstrap';
import axios from 'axios';
import './index.css';

const App = () => {
  const searchInput = useRef(null);
  const imageInput = useRef(null);

  const [images, setImages] = useState([]);
  const [selectedImage, setSelectedImage] = useState(null);

  const handleTextSearch = (event) => {
    event.preventDefault();
    const searchValue = searchInput.current.value;

    // clear results
    setImages([]);
    // clear form fields
    imageInput.current.value = '';
    // clear file preview
    setSelectedImage(null);

    fetchImagesbyText(searchValue);
  };

  const handleImageChange = (event) => {
    const file = event.target.files[0];
    const reader = new FileReader();

    reader.onloadend = () => {
      setSelectedImage(reader.result);
    };
    reader.readAsDataURL(file);
  }

  const fetchImagesbyText = async (searchValue) => {
    try {
      const { data } = await axios.post(process.env.REACT_APP_AZURE_TEXT_API_URL, {
        text: searchValue
      })
      setImages(data);
    } catch (error) {
      console.log(error);
    }
  };

  const handleImageSearch = async (event) => {
    event.preventDefault();
    // clear search field
    searchInput.current.value = '';

    const formData = new FormData();
    formData.append('image', event.target[0].files[0]);

    fetchImagesbyImage(formData);
  }

  const fetchImagesbyImage = async (formData) => {
    try {
      const { data } = await axios.post(process.env.REACT_APP_AZURE_IMAGE_API_URL, formData,
      {
        headers: {
            "Content-type": "multipart/form-data",
        }})
      setImages(data);
    } catch (error) {
      console.log(error);
    }
  };

  return (
    <div className='container'>
      <h1 className='title'>Image Search</h1>
      <div className='search-section'>
        <Form onSubmit={handleImageSearch}>
            <Form.Label className='fw-bold'>Search by image</Form.Label>
            <InputGroup className='mb-3'>
              <Form.Control
                type="file"
                size="lg"
                accept="image/*"
                capture="environment"
                onChange={handleImageChange}
                ref={imageInput}
              />
              <Button type='submit' variant='primary'>
                  Search
              </Button>
            </InputGroup>
            {selectedImage && <img src={selectedImage} alt='Preview' className='imagePreview mb-3' width='200' height='200'/>}
        </Form>
      </div>

      <div className='search-section'>
        <Form onSubmit={handleTextSearch}>
          <Form.Label className='fw-bold'>Search by text</Form.Label>
            <InputGroup className='mb-3'>
              <Form.Control
                type='search'
                placeholder='Text search (e.g., a mood, favorite color, keywords, or phrases)'
                className='search-input'
                ref={searchInput}
              />
              <Button type='submit' variant='primary'>
                  Search
              </Button>
            </InputGroup>
        </Form>
      </div>

      <div className='images'>
      {images.map((result) => {
        return (
          <div key={result.objectId}>
            <img
              key={result.objectId}
              src={result.imageUrl}
              alt={result.title}
              className='image'
            />
            <div className='imageText'>
              <div className='header' dangerouslySetInnerHTML={{ __html: result.title}}></div>
              <div className='detail'>{result.artist === '' ? 'Artist: Unknown' : 'Artist: ' + result.artist}</div>
              <div className='detail'>Score: {result.similarityScore}</div>
            </div>
          </div>
        );
      })}
    </div>
    </div>
  );
};

export default App;