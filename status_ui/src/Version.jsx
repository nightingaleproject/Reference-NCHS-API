import React from 'react';
import { Typography, Box } from '@mui/material';

export const Version = () => {
  const STATUS_UI_VERSION = '1.0.1';
  return (
    <Box sx={{ display: 'flex', justifyContent: 'center' }}>
      <Typography variant="h6" sx={{ my: 2, fontSize: '1.0rem' }}>
        v{STATUS_UI_VERSION}
      </Typography>
    </Box>
  );
};

export default Version;
