import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Pagination } from './Pagination';

describe('Pagination', () => {
  it('muestra el rango y deshabilita "anterior" en la primera página', () => {
    render(<Pagination page={1} totalPages={3} totalCount={25} pageSize={10} onPageChange={() => {}} />);

    expect(screen.getByRole('navigation')).toHaveTextContent('Mostrando 1–10 de 25');
    expect(screen.getByLabelText('Página anterior')).toBeDisabled();
    expect(screen.getByLabelText('Página siguiente')).toBeEnabled();
  });

  it('avanza de página', async () => {
    const onPageChange = vi.fn();
    render(<Pagination page={2} totalPages={3} totalCount={25} pageSize={10} onPageChange={onPageChange} />);

    await userEvent.click(screen.getByLabelText('Página siguiente'));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });

  it('en la última página muestra el total parcial', () => {
    render(<Pagination page={3} totalPages={3} totalCount={25} pageSize={10} onPageChange={() => {}} />);
    expect(screen.getByRole('navigation')).toHaveTextContent('Mostrando 21–25 de 25');
    expect(screen.getByLabelText('Página siguiente')).toBeDisabled();
  });

  it('no se muestra sin resultados', () => {
    const { container } = render(<Pagination page={1} totalPages={0} totalCount={0} pageSize={10} onPageChange={() => {}} />);
    expect(container).toBeEmptyDOMElement();
  });
});
